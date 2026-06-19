using EventosVivos.Api.IntegrationTests.Soporte;
using EventosVivos.Application.Eventos;
using EventosVivos.Domain.Eventos;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Repositories;

namespace EventosVivos.Api.IntegrationTests.Repositorios;

[Trait("Category", "Repositorio")]
[Collection("postgres")]
public class EventoRepositoryTests
{
    private readonly PostgresFixture _fixture;
    private static readonly DateTimeOffset Base = Datos.Base;

    public EventoRepositoryTests(PostgresFixture fixture) => _fixture = fixture;

    private async Task<EventosDbContext> GuardarAsync(params Evento[] eventos)
    {
        await using (var db = _fixture.CrearContexto())
        {
            db.Eventos.AddRange(eventos);
            await db.SaveChangesAsync();
        }
        return _fixture.CrearContexto();
    }

    // ---- Regresión: persistir fechas con offset no-UTC (Npgsql exige UTC en timestamptz) ----
    [Fact]
    public async Task Persistir_evento_con_offset_no_utc_lo_normaliza_a_UTC()
    {
        await _fixture.LimpiarEventosAsync();
        // El frontend envía la hora local del venue (p. ej. Colombia -05:00) para validar RN03.
        var inicioCo = new DateTimeOffset(2026, 9, 15, 14, 0, 0, TimeSpan.FromHours(-5));
        var evento = Datos.CrearEvento(venueId: 1, inicio: inicioCo, duracion: TimeSpan.FromHours(2));

        await using var db = await GuardarAsync(evento); // no debe lanzar

        var recuperado = await new EventoRepository(db).ObtenerPorIdAsync(evento.Id);
        recuperado!.FechaInicio.Offset.Should().Be(TimeSpan.Zero); // almacenado/recuperado en UTC
        recuperado.FechaInicio.Should().Be(inicioCo.ToUniversalTime()); // mismo instante
    }

    // ---- RN02: superposición de venues (bordes) ----
    [Fact]
    public async Task ExisteSolapamiento_detecta_intervalos_que_se_solapan()
    {
        await _fixture.LimpiarEventosAsync();
        var evento = Datos.CrearEvento(venueId: 1, inicio: Base, duracion: TimeSpan.FromHours(2));
        await using var db = await GuardarAsync(evento);
        var repo = new EventoRepository(db);

        var solapa = await repo.ExisteSolapamientoAsync(1, Base.AddHours(1), Base.AddHours(3));

        solapa.Should().BeTrue();
    }

    [Fact]
    public async Task ExisteSolapamiento_contacto_en_extremos_no_cuenta()
    {
        await _fixture.LimpiarEventosAsync();
        var evento = Datos.CrearEvento(venueId: 1, inicio: Base, duracion: TimeSpan.FromHours(2));
        await using var db = await GuardarAsync(evento);
        var repo = new EventoRepository(db);

        // nuevo intervalo empieza justo cuando el anterior termina (Base+2h)
        var solapa = await repo.ExisteSolapamientoAsync(1, Base.AddHours(2), Base.AddHours(4));

        solapa.Should().BeFalse();
    }

    [Fact]
    public async Task ExisteSolapamiento_otro_venue_no_cuenta()
    {
        await _fixture.LimpiarEventosAsync();
        var evento = Datos.CrearEvento(venueId: 1, inicio: Base, duracion: TimeSpan.FromHours(2));
        await using var db = await GuardarAsync(evento);
        var repo = new EventoRepository(db);

        var solapa = await repo.ExisteSolapamientoAsync(2, Base.AddHours(1), Base.AddHours(3));

        solapa.Should().BeFalse();
    }

    [Fact]
    public async Task ExisteSolapamiento_excluye_el_propio_evento()
    {
        await _fixture.LimpiarEventosAsync();
        var evento = Datos.CrearEvento(venueId: 1, inicio: Base, duracion: TimeSpan.FromHours(2));
        await using var db = await GuardarAsync(evento);
        var repo = new EventoRepository(db);

        var solapa = await repo.ExisteSolapamientoAsync(1, Base.AddHours(1), Base.AddHours(3), excluirEventoId: evento.Id);

        solapa.Should().BeFalse();
    }

    [Fact]
    public async Task ExisteSolapamiento_evento_cancelado_no_cuenta()
    {
        await _fixture.LimpiarEventosAsync();
        var evento = Datos.CrearEvento(venueId: 3, inicio: Base, duracion: TimeSpan.FromHours(2));
        evento.Cancelar(Datos.Reloj);
        await using var db = await GuardarAsync(evento);
        var repo = new EventoRepository(db);

        var solapa = await repo.ExisteSolapamientoAsync(3, Base.AddHours(1), Base.AddHours(3));

        solapa.Should().BeFalse();
    }

    // ---- RF-02: búsqueda de título con ILIKE (case-insensitive, parcial) ----
    [Fact]
    public async Task ListarAsync_busca_titulo_con_ILIKE_case_insensitive()
    {
        await _fixture.LimpiarEventosAsync();
        await GuardarAsync(
            Datos.CrearEvento(titulo: "Jazz Festival", inicio: Base),
            Datos.CrearEvento(titulo: "Concierto de Rock", inicio: Base.AddDays(1)),
            Datos.CrearEvento(titulo: "JAZZ Nocturno", inicio: Base.AddDays(2)));

        await using var db = _fixture.CrearContexto();
        var repo = new EventoRepository(db);

        var (items, total) = await repo.ListarAsync(new EventoFiltro { Titulo = "jazz" });

        total.Should().Be(2);
        items.Should().OnlyContain(e => e.Titulo.Contains("Jazz", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ListarAsync_filtra_por_tipo_y_venue()
    {
        await _fixture.LimpiarEventosAsync();
        await GuardarAsync(
            Datos.CrearEvento(titulo: "Taller A", tipo: TipoEvento.Taller, venueId: 1, inicio: Base),
            Datos.CrearEvento(titulo: "Concierto B", tipo: TipoEvento.Concierto, venueId: 1, inicio: Base.AddDays(1)),
            Datos.CrearEvento(titulo: "Taller C", tipo: TipoEvento.Taller, venueId: 2, inicio: Base.AddDays(2)));

        await using var db = _fixture.CrearContexto();
        var repo = new EventoRepository(db);

        var (porTipo, totalTipo) = await repo.ListarAsync(new EventoFiltro { Tipo = TipoEvento.Taller });
        var (porVenue, totalVenue) = await repo.ListarAsync(new EventoFiltro { VenueId = 2 });

        totalTipo.Should().Be(2);
        totalVenue.Should().Be(1);
        porTipo.Should().OnlyContain(e => e.Tipo == TipoEvento.Taller);
        porVenue.Should().OnlyContain(e => e.VenueId == 2);
    }

    // ---- ObtenerPorReserva: carga el agregado a partir de una reserva ----
    [Fact]
    public async Task ObtenerPorReserva_devuelve_el_evento_con_sus_reservas()
    {
        await _fixture.LimpiarEventosAsync();
        var evento = Datos.CrearEvento(capacidad: 100, inicio: Base);
        var reserva = evento.Reservar(3, "Ana Pérez", Datos.Email(), Datos.Reloj);
        await using var db = await GuardarAsync(evento);
        var repo = new EventoRepository(db);

        var recuperado = await repo.ObtenerPorReservaAsync(reserva.Id);

        recuperado.Should().NotBeNull();
        recuperado!.Id.Should().Be(evento.Id);
        recuperado.Reservas.Should().ContainSingle(r => r.Id == reserva.Id);
        recuperado.Reservas.Single().EmailComprador.Valor.Should().Be("comprador@correo.com");
    }
}
