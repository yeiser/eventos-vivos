using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Venues;

namespace EventosVivos.Domain.Tests.Eventos;

public class EventoCrearTests
{
    private readonly RelojFalso _reloj = Dominio.Reloj();

    [Fact]
    public void Crear_evento_valido_queda_activo_con_sus_datos()
    {
        var inicio = _reloj.Now.AddDays(5);
        var evento = Evento.Crear(
            "Jazz en el Auditorio", "Una noche de jazz con artistas locales e internacionales.",
            Dominio.Venue(200), 180, inicio, inicio.AddHours(3), 120m, TipoEvento.Concierto, _reloj);

        evento.Estado.Should().Be(EstadoEvento.Activo);
        evento.CapacidadMaxima.Should().Be(180);
        evento.EntradasDisponibles.Should().Be(180);
        evento.Id.Should().NotBe(Guid.Empty);
    }

    // ---- RF-01: longitud de título (5-100) ----
    [Theory]
    [InlineData(4)]
    [InlineData(101)]
    public void Crear_con_titulo_fuera_de_rango_lanza_DatosInvalidos(int longitud)
    {
        var act = () => Dominio.Evento(_reloj, titulo: new string('a', longitud));

        act.Should().Throw<DatosInvalidosException>();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    public void Crear_con_titulo_en_los_limites_es_valido(int longitud)
    {
        var act = () => Dominio.Evento(_reloj, titulo: new string('a', longitud));

        act.Should().NotThrow();
    }

    // ---- RF-01: longitud de descripción (10-500) ----
    [Theory]
    [InlineData(9)]
    [InlineData(501)]
    public void Crear_con_descripcion_fuera_de_rango_lanza_DatosInvalidos(int longitud)
    {
        var act = () => Dominio.Evento(_reloj, descripcion: new string('a', longitud));

        act.Should().Throw<DatosInvalidosException>();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(500)]
    public void Crear_con_descripcion_en_los_limites_es_valido(int longitud)
    {
        var act = () => Dominio.Evento(_reloj, descripcion: new string('a', longitud));

        act.Should().NotThrow();
    }

    // ---- RN01: capacidad <= capacidad del venue ----
    [Fact]
    public void Crear_con_capacidad_igual_a_la_del_venue_es_valido()
    {
        var act = () => Dominio.Evento(_reloj, venue: Dominio.Venue(200), capacidad: 200);

        act.Should().NotThrow();
    }

    [Fact]
    public void Crear_con_capacidad_mayor_a_la_del_venue_lanza_RN01()
    {
        var act = () => Dominio.Evento(_reloj, venue: Dominio.Venue(200), capacidad: 201);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN01");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Crear_con_capacidad_no_positiva_lanza_DatosInvalidos(int capacidad)
    {
        var act = () => Dominio.Evento(_reloj, capacidad: capacidad);

        act.Should().Throw<DatosInvalidosException>();
    }

    // ---- RF-01: precio positivo ----
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Crear_con_precio_no_positivo_lanza_DatosInvalidos(int precio)
    {
        var act = () => Dominio.Evento(_reloj, precio: precio);

        act.Should().Throw<DatosInvalidosException>();
    }

    // ---- RF-01: fecha futura y fin > inicio ----
    [Fact]
    public void Crear_con_inicio_no_futuro_lanza_DatosInvalidos()
    {
        var act = () => Dominio.Evento(_reloj, inicio: _reloj.Now); // == ahora

        act.Should().Throw<DatosInvalidosException>();
    }

    [Fact]
    public void Crear_con_inicio_en_el_pasado_lanza_DatosInvalidos()
    {
        var act = () => Dominio.Evento(_reloj, inicio: _reloj.Now.AddMinutes(-1));

        act.Should().Throw<DatosInvalidosException>();
    }

    [Fact]
    public void Crear_con_fin_anterior_o_igual_al_inicio_lanza_DatosInvalidos()
    {
        var inicio = _reloj.Now.AddDays(3);
        var act = () => Evento.Crear("Titulo válido", "Descripción válida y larga.",
            Dominio.Venue(), 50, inicio, inicio, 30m, TipoEvento.Taller, _reloj);

        act.Should().Throw<DatosInvalidosException>();
    }

    // ---- RF-01: tipo válido ----
    [Fact]
    public void Crear_con_tipo_invalido_lanza_DatosInvalidos()
    {
        var act = () => Dominio.Evento(_reloj, tipo: (TipoEvento)999);

        act.Should().Throw<DatosInvalidosException>();
    }

    // ---- RN03: horario nocturno en fin de semana (después de las 22:00) ----
    // 2026-06-20 es sábado; 2026-06-21 domingo; 2026-06-22 lunes.
    [Theory]
    [InlineData("2026-06-20", 22, 1)]   // sábado 22:01 -> rechazo
    [InlineData("2026-06-21", 23, 0)]   // domingo 23:00 -> rechazo
    public void Crear_en_fin_de_semana_despues_de_las_22_lanza_RN03(string fecha, int hora, int minuto)
    {
        var inicio = ConFecha(fecha, hora, minuto);
        var act = () => Dominio.Evento(_reloj, inicio: inicio);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN03");
    }

    [Theory]
    [InlineData("2026-06-20", 22, 0)]   // sábado 22:00 exacto -> permitido (estricto)
    [InlineData("2026-06-20", 21, 59)]  // sábado 21:59 -> permitido
    [InlineData("2026-06-21", 20, 0)]   // domingo 20:00 -> permitido
    [InlineData("2026-06-22", 23, 0)]   // lunes 23:00 -> permitido (no aplica fin de semana)
    public void Crear_horario_permitido_no_lanza_RN03(string fecha, int hora, int minuto)
    {
        var inicio = ConFecha(fecha, hora, minuto);
        var act = () => Dominio.Evento(_reloj, inicio: inicio);

        act.Should().NotThrow();
    }

    private static DateTimeOffset ConFecha(string fecha, int hora, int minuto)
    {
        var d = DateOnly.Parse(fecha);
        return new DateTimeOffset(d.Year, d.Month, d.Day, hora, minuto, 0, Dominio.OffsetCo);
    }
}
