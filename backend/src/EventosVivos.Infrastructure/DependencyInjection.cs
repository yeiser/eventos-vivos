using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Reservas;
using EventosVivos.Infrastructure.Auth;
using EventosVivos.Infrastructure.Identity;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Persistence.Interceptors;
using EventosVivos.Infrastructure.Persistence.Seed;
using EventosVivos.Infrastructure.Repositories;
using EventosVivos.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventosVivos.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Postgres";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var conexion = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"No se encontró la cadena de conexión '{ConnectionStringName}'.");

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<AuditLogSaveChangesInterceptor>();

        services.AddDbContext<EventosDbContext>((sp, options) => options.ConfigurarPostgres(conexion, sp));

        // Actor por defecto ("system"); la API registra antes su CurrentUserService basado en HttpContext.
        services.TryAddScoped<ICurrentUserService, SystemCurrentUserService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventoRepository, EventoRepository>();
        services.AddScoped<IReservaRepository, ReservaRepository>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        services.AddScoped<IReservationCodeGenerator, GeneradorCodigoReserva>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Seccion));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    /// <summary>
    /// Configura el DbContext de PostgreSQL con la convención snake_case y los interceptores de
    /// auditoría (trazabilidad + audit trail), resolviéndolos del contenedor para que compartan el
    /// scope de la petición (y por tanto el actor actual).
    /// </summary>
    public static DbContextOptionsBuilder ConfigurarPostgres(
        this DbContextOptionsBuilder options, string conexion, IServiceProvider serviceProvider) =>
        options
            .UseNpgsql(conexion)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<AuditLogSaveChangesInterceptor>());

    /// <summary>Aplica migraciones pendientes y siembra los datos de referencia (arranque en dev).</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventosDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
