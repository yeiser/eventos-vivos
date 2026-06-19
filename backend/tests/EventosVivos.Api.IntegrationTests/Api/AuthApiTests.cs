using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventosVivos.Api.IntegrationTests.Soporte;

namespace EventosVivos.Api.IntegrationTests.Api;

[Trait("Category", "Auth")]
[Collection("api")]
public class AuthApiTests
{
    private readonly ApiFactory _factory;

    public AuthApiTests(ApiFactory factory) => _factory = factory;

    private static int _contadorDias = 200;

    // Eventos en venue 2 para no chocar (RN02) con los de EventosApiTests (venue 1).
    private static object EventoBody(int capacidad = 50)
    {
        var dias = Interlocked.Increment(ref _contadorDias);
        var inicio = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(dias).AddHours(14), TimeSpan.Zero);
        return new
        {
            titulo = "Evento Auth",
            descripcion = "Descripción de prueba suficientemente larga.",
            venueId = 2,
            capacidadMaxima = capacidad,
            fechaInicio = inicio,
            fechaFin = inicio.AddHours(2),
            precio = 40m,
            tipo = "taller"
        };
    }

    // ---- Login ----
    [Fact]
    public async Task Login_admin_devuelve_token_y_rol()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new { nombreUsuario = "admin", password = "Admin123!" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("rol").GetString().Should().Be("Admin");
    }

    [Fact]
    public async Task Login_con_password_incorrecta_devuelve_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new { nombreUsuario = "admin", password = "incorrecta" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_usuario_inexistente_devuelve_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", new { nombreUsuario = "fantasma", password = "x" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- Autenticación requerida ----
    [Fact]
    public async Task Endpoint_protegido_sin_token_devuelve_401_problem_json()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/venues");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Usuario_autenticado_puede_listar_venues()
    {
        var client = await _factory.CrearClienteUsuarioAsync();

        var resp = await client.GetAsync("/api/v1/venues");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---- Autorización por rol ----
    [Fact]
    public async Task Usuario_no_admin_no_puede_crear_evento_403()
    {
        var client = await _factory.CrearClienteUsuarioAsync();

        var resp = await client.PostAsJsonAsync("/api/v1/eventos", EventoBody());

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Usuario_puede_reservar_en_evento_creado_por_admin()
    {
        var admin = await _factory.CrearClienteAdminAsync();
        var crear = await admin.PostAsJsonAsync("/api/v1/eventos", EventoBody());
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var usuario = await _factory.CrearClienteUsuarioAsync();
        var reservar = await usuario.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 1, nombreComprador = "Ana", emailComprador = "ana@correo.com" });

        reservar.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Usuario_no_admin_no_puede_confirmar_pago_403()
    {
        var admin = await _factory.CrearClienteAdminAsync();
        var crear = await admin.PostAsJsonAsync("/api/v1/eventos", EventoBody());
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var usuario = await _factory.CrearClienteUsuarioAsync();
        var reservar = await usuario.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 1, nombreComprador = "Ana", emailComprador = "ana@correo.com" });
        var reservaId = (await reservar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var confirmar = await usuario.PostAsync($"/api/v1/reservas/{reservaId}/confirmacion", null);

        confirmar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
