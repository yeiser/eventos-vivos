using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EventosVivos.Api.IntegrationTests.Soporte;

namespace EventosVivos.Api.IntegrationTests.Api;

[Trait("Category", "Api")]
[Collection("api")]
public class EventosApiTests
{
    private readonly ApiFactory _factory;

    public EventosApiTests(ApiFactory factory) => _factory = factory;

    private static int _contadorDias;

    private static object EventoValido(string titulo = "Concierto de prueba", int venueId = 1, int capacidad = 100, decimal precio = 50m)
    {
        // Cada evento usa un día futuro distinto para no chocar con RN02 (solapamiento en el mismo venue).
        var dias = 30 + Interlocked.Increment(ref _contadorDias);
        var inicio = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(dias).AddHours(14), TimeSpan.Zero);
        return new
        {
            titulo,
            descripcion = "Descripción de prueba suficientemente larga.",
            venueId,
            capacidadMaxima = capacidad,
            fechaInicio = inicio,
            fechaFin = inicio.AddHours(2),
            precio,
            tipo = "concierto"
        };
    }

    // ---- Flujo completo (happy path): crear → reservar → confirmar → reporte ----
    [Fact]
    public async Task Flujo_completo_de_evento_reserva_confirmacion_y_reporte()
    {
        var client = await _factory.CrearClienteAdminAsync();

        // venues sembrados
        var venues = await client.GetFromJsonAsync<JsonElement>("/api/v1/venues");
        venues.GetArrayLength().Should().Be(3);

        // crear evento
        var crear = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Jazz Flow"));
        crear.StatusCode.Should().Be(HttpStatusCode.Created);
        var evento = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var eventoId = evento.GetProperty("id").GetGuid();
        evento.GetProperty("estado").GetString().Should().Be("activo");

        // reservar
        var reservar = await client.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 2, nombreComprador = "Ana Pérez", emailComprador = "ana@correo.com" });
        reservar.StatusCode.Should().Be(HttpStatusCode.Created);
        var reserva = await reservar.Content.ReadFromJsonAsync<JsonElement>();
        reserva.GetProperty("estado").GetString().Should().Be("pendiente_pago");
        var reservaId = reserva.GetProperty("id").GetGuid();

        // confirmar pago
        var confirmar = await client.PostAsync($"/api/v1/reservas/{reservaId}/confirmacion", null);
        confirmar.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmada = await confirmar.Content.ReadFromJsonAsync<JsonElement>();
        confirmada.GetProperty("estado").GetString().Should().Be("confirmada");
        confirmada.GetProperty("codigo").GetString().Should().MatchRegex(@"^EV-\d{6}$");

        // reporte
        var reporte = await client.GetFromJsonAsync<JsonElement>($"/api/v1/eventos/{eventoId}/reporte");
        reporte.GetProperty("entradasVendidas").GetInt32().Should().Be(2);
        reporte.GetProperty("entradasDisponibles").GetInt32().Should().Be(98);
        reporte.GetProperty("ingresosTotales").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task Listar_eventos_filtra_por_titulo()
    {
        var client = await _factory.CrearClienteAdminAsync();
        await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Festival Único XYZ"));

        var resultado = await client.GetFromJsonAsync<JsonElement>("/api/v1/eventos?titulo=XYZ");

        resultado.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        resultado.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(e => e.GetProperty("titulo").GetString()!.Contains("XYZ"));
    }

    // ---- Errores ----
    [Fact]
    public async Task Crear_evento_invalido_devuelve_400_problem_json()
    {
        var client = await _factory.CrearClienteAdminAsync();

        var resp = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "abc"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Obtener_evento_inexistente_devuelve_404()
    {
        var client = await _factory.CrearClienteAdminAsync();

        var resp = await client.GetAsync($"/api/v1/eventos/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Reservar_mas_que_el_aforo_devuelve_422_con_regla()
    {
        var client = await _factory.CrearClienteAdminAsync();
        var crear = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Aforo 1", capacidad: 1));
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 2, nombreComprador = "Ana", emailComprador = "ana@correo.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("regla").GetString().Should().Be("CAPACIDAD");
    }

    [Fact]
    public async Task Reservar_cantidad_invalida_devuelve_400()
    {
        var client = await _factory.CrearClienteAdminAsync();
        var crear = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Cantidad Inv"));
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resp = await client.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 0, nombreComprador = "Ana", emailComprador = "ana@correo.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirmar_dos_veces_devuelve_409()
    {
        var client = await _factory.CrearClienteAdminAsync();
        var crear = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Doble Confirm"));
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var reservar = await client.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 1, nombreComprador = "Ana", emailComprador = "ana@correo.com" });
        var reservaId = (await reservar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PostAsync($"/api/v1/reservas/{reservaId}/confirmacion", null);
        var segunda = await client.PostAsync($"/api/v1/reservas/{reservaId}/confirmacion", null);

        segunda.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Confirmar_reserva_inexistente_devuelve_404()
    {
        var client = await _factory.CrearClienteAdminAsync();

        var resp = await client.PostAsync($"/api/v1/reservas/{Guid.NewGuid()}/confirmacion", null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Listar_reservas_de_un_evento_devuelve_sus_reservas()
    {
        var client = await _factory.CrearClienteAdminAsync();
        var crear = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Con Reservas"));
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 2, nombreComprador = "Ana", emailComprador = "ana@correo.com" });

        var reservas = await client.GetFromJsonAsync<JsonElement>($"/api/v1/eventos/{eventoId}/reservas");

        reservas.GetArrayLength().Should().Be(1);
        reservas[0].GetProperty("nombreComprador").GetString().Should().Be("Ana");
    }

    [Fact]
    public async Task Buscar_reservas_por_codigo_y_por_nombre()
    {
        var client = await _factory.CrearClienteAdminAsync();
        var crear = await client.PostAsJsonAsync("/api/v1/eventos", EventoValido(titulo: "Evento Buscable"));
        var eventoId = (await crear.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var reservar = await client.PostAsJsonAsync($"/api/v1/eventos/{eventoId}/reservas",
            new { cantidad = 1, nombreComprador = "Comprador Unico ZZQ", emailComprador = "zzq@correo.com" });
        var reservaId = (await reservar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var confirmar = await client.PostAsync($"/api/v1/reservas/{reservaId}/confirmacion", null);
        var codigo = (await confirmar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString();

        var porNombre = await client.GetFromJsonAsync<JsonElement>("/api/v1/reservas?nombreComprador=ZZQ");
        porNombre.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        porNombre.GetProperty("items")[0].GetProperty("eventoTitulo").GetString().Should().Be("Evento Buscable");

        var porCodigo = await client.GetFromJsonAsync<JsonElement>($"/api/v1/reservas?codigo={codigo}");
        porCodigo.GetProperty("total").GetInt32().Should().Be(1);
        porCodigo.GetProperty("items")[0].GetProperty("codigo").GetString().Should().Be(codigo);
    }
}
