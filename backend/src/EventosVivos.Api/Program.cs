using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using EventosVivos.Api.Errores;
using EventosVivos.Api.Identity;
using EventosVivos.Api.Swagger;
using EventosVivos.Application;
using EventosVivos.Application.Abstractions;
using EventosVivos.Infrastructure;
using EventosVivos.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

const string CorsPolicy = "frontend";
var origenesCors = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    // Enums como strings snake_case (conferencia, pendiente_pago, ...), según el enunciado.
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(ConfigurarSwagger);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(origenesCors)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("reservas", PorIp(permisos: 100, ventanaSegundos: 10));
    options.AddPolicy("auth", PorIp(permisos: 50, ventanaSegundos: 10));
});

// Autenticación JWT (ADR-007).
var jwt = builder.Configuration.GetSection(JwtOptions.Seccion).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la configuración 'Jwt'.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Respuestas ProblemDetails para 401/403 del middleware.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await RespuestaProblema.EscribirAsync(context.HttpContext,
                    StatusCodes.Status401Unauthorized, "no-autenticado",
                    "No autenticado", "Se requiere un token de acceso válido.");
            },
            OnForbidden = context => RespuestaProblema.EscribirAsync(context.HttpContext,
                StatusCodes.Status403Forbidden, "acceso-denegado",
                "Acceso denegado", "No tiene permisos para realizar esta acción.")
        };
    });

// Por defecto, todos los endpoints requieren autenticación (salvo [AllowAnonymous]).
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.DocumentTitle = "EventosVivos API";
        ui.DisplayRequestDuration();          // muestra la duración de cada petición
        ui.EnablePersistAuthorization();      // conserva el token al refrescar la página
        ui.DefaultModelsExpandDepth(0);       // colapsa la sección de esquemas
    });
}

// Migración + seed al arrancar. Automático en Development; en contenedores/producción se
// activa con Database:AutoMigrate=true (en Azure las migraciones pueden correr como paso aparte).
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:AutoMigrate"))
{
    await app.Services.InitializeDatabaseAsync();
}

// Detrás de Nginx/Ingress el TLS se termina en el proxy; dentro del contenedor la API es HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static Func<HttpContext, RateLimitPartition<string>> PorIp(int permisos, int ventanaSegundos) =>
    httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permisos,
            Window = TimeSpan.FromSeconds(ventanaSegundos)
        });

static void ConfigurarSwagger(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventosVivos API",
        Version = "v1",
        Description = """
            API REST del núcleo de reservas de eventos: control de aforo **sin sobreventa**,
            prevención de conflictos de agenda por venue y automatización de la validación de pagos.

            **Autenticación** — obtén un JWT en `POST /api/v1/auth/login` y púlsalo en **Authorize** 🔒.
            Credenciales de demo: `admin` / `Admin123!` (Admin) · `usuario` / `Usuario123!` (Usuario).
            Los endpoints con candado requieren token; los de rol **Admin** lo indican en su descripción.

            **Errores** — formato `application/problem+json` (RFC 7807) con código de regla y `traceId`.
            """,
        Contact = new OpenApiContact { Name = "EventosVivos" },
    });

    // Esquema Bearer: en el diálogo «Authorize» basta con pegar el token (sin escribir "Bearer ").
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el JWT obtenido en POST /api/v1/auth/login (sin el prefijo 'Bearer ').",
    });

    // El candado se aplica POR operación (solo a las protegidas), no de forma global.
    options.OperationFilter<SecurityRequirementsOperationFilter>();

    // Incluye los comentarios XML (summary/remarks/response) en la documentación.
    var xml = Path.Combine(AppContext.BaseDirectory, "EventosVivos.Api.xml");
    if (File.Exists(xml))
        options.IncludeXmlComments(xml, includeControllerXmlComments: true);

    options.SupportNonNullableReferenceTypes();
    options.DescribeAllParametersInCamelCase();
}

// Punto de entrada visible para las pruebas de integración (WebApplicationFactory).
public partial class Program;
