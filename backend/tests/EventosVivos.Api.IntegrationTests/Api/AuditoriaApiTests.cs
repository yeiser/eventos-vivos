using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventosVivos.Api.IntegrationTests.Soporte;

namespace EventosVivos.Api.IntegrationTests.Api;

[Trait("Category", "Auditoria")]
[Collection("api")]
public class AuditoriaApiTests
{
    private readonly ApiFactory _factory;

    public AuditoriaApiTests(ApiFactory factory) => _factory = factory;

    private static int _contadorDias = 400;

    // Eventos en venue 3 para no chocar (RN02) con los de otras clases de prueba.
    private static object EventoBody(int capacidad = 100)
    {
        var dias = Interlocked.Increment(ref _contadorDias);
        var inicio = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(dias).AddHours(14), TimeSpan.Zero);
        return new
        {
            titulo = "Evento Auditoría",
            descripcion = "Descripción de prueba suficientemente larga.",
            venueId = 3,
            capacidadMaxima = capacidad,
            fechaInicio = inicio,
            fechaFin = inicio.AddHours(2),
            precio = 60m,
            tipo = "conferencia"
        };
    }

    // ---- Trazabilidad (§16.1) ----
    [Fact]
    public async Task Crear_evento_rellena_creadoPor_con_el_actor()
    {
        var admin = await _factory.CrearClienteAdminAsync();
        var crear = await admin.PostAsJsonAsync("/api/v1/eventos", EventoBody());
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var evento = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/eventos/{eventoId}");
        var auditoria = evento.GetProperty("auditoria");

        auditoria.GetProperty("creadoPor").GetString().Should().Be("admin");
    }

    [Fact]
    public async Task Confirmar_reserva_actualiza_modificadoPor()
    {
        var admin = await _factory.CrearClienteAdminAsync();
        var crear = await admin.PostAsJsonAsync("/api/v1/eventos", EventoBody());
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var reservar = await admin.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 1, nombreComprador = "Ana", emailComprador = "ana@correo.com" });
        var reservaId = (await reservar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await admin.PostAsync($"/api/v1/reservas/{reservaId}/confirmacion", null);

        var reserva = await admin.GetFromJsonAsync<JsonElement>($"/api/v1/reservas/{reservaId}");
        reserva.GetProperty("auditoria").GetProperty("modificadoPor").GetString().Should().Be("admin");
    }

    // ---- Audit trail (§16.2) ----
    [Fact]
    public async Task Crear_evento_genera_exactamente_un_registro_de_auditoria()
    {
        var admin = await _factory.CrearClienteAdminAsync();
        var crear = await admin.PostAsJsonAsync("/api/v1/eventos", EventoBody());
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var auditoria = await admin.GetFromJsonAsync<JsonElement>(
            $"/api/v1/auditoria?entidad=Evento&entidadId={eventoId}");

        auditoria.GetProperty("total").GetInt32().Should().Be(1);
        var registro = auditoria.GetProperty("items").EnumerateArray().Single();
        registro.GetProperty("accion").GetString().Should().Be("crear");
        registro.GetProperty("usuario").GetString().Should().Be("admin");
        registro.GetProperty("valoresNuevos").GetString().Should().Contain("Evento Auditoría");
    }

    // ---- Masking (§16.2) ----
    [Fact]
    public async Task El_audit_trail_enmascara_el_hash_de_contrasena_de_los_usuarios()
    {
        var admin = await _factory.CrearClienteAdminAsync();

        var auditoria = await admin.GetFromJsonAsync<JsonElement>("/api/v1/auditoria?entidad=Usuario");

        auditoria.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        foreach (var registro in auditoria.GetProperty("items").EnumerateArray())
        {
            var valores = registro.GetProperty("valoresNuevos").GetString();
            valores.Should().Contain("***");
            valores.Should().NotContain("Admin123!");
            valores.Should().NotContain("Usuario123!");
        }
    }

    // ---- Autorización (§16.4) ----
    [Fact]
    public async Task Auditoria_sin_token_devuelve_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/auditoria");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Auditoria_como_usuario_devuelve_403()
    {
        var usuario = await _factory.CrearClienteUsuarioAsync();

        var resp = await usuario.GetAsync("/api/v1/auditoria");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
