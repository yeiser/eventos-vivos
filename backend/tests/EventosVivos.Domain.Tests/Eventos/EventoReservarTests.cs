using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Domain.Tests.Eventos;

public class EventoReservarTests
{
    private readonly RelojFalso _reloj = Dominio.Reloj();

    private Evento EventoConInicioEn(TimeSpan antelacion, decimal precio = 50m, int capacidad = 100) =>
        Dominio.Evento(_reloj, capacidad: capacidad, precio: precio,
            inicio: _reloj.Now + antelacion, duracion: TimeSpan.FromHours(2));

    [Fact]
    public void Reservar_valido_crea_reserva_pendiente_y_ocupa_cupo()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5));

        var reserva = evento.Reservar(3, "Ana Pérez", Dominio.Email(), _reloj);

        reserva.Estado.Should().Be(EstadoReserva.PendientePago);
        reserva.Cantidad.Should().Be(3);
        evento.EntradasOcupadas.Should().Be(3);
        evento.EntradasDisponibles.Should().Be(97);
        evento.Reservas.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reservar_cantidad_menor_a_1_lanza_DatosInvalidos(int cantidad)
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5));

        var act = () => evento.Reservar(cantidad, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<DatosInvalidosException>();
    }

    // ---- RN04: reserva tardía (< 1 hora) ----
    [Fact]
    public void Reservar_con_menos_de_una_hora_para_inicio_lanza_RN04()
    {
        var evento = EventoConInicioEn(TimeSpan.FromMinutes(59));

        var act = () => evento.Reservar(1, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN04");
    }

    [Theory]
    [InlineData(60)]   // exactamente 1h -> permitido
    [InlineData(61)]
    public void Reservar_con_una_hora_o_mas_no_lanza_RN04(int minutos)
    {
        var evento = EventoConInicioEn(TimeSpan.FromMinutes(minutos));

        var act = () => evento.Reservar(1, "Ana", Dominio.Email(), _reloj);

        act.Should().NotThrow();
    }

    // ---- RF-03: < 24h -> máximo 5 por transacción ----
    [Fact]
    public void Reservar_evento_proximo_permite_hasta_5()
    {
        var evento = EventoConInicioEn(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59));

        var act = () => evento.Reservar(5, "Ana", Dominio.Email(), _reloj);

        act.Should().NotThrow();
    }

    [Fact]
    public void Reservar_evento_proximo_mas_de_5_lanza_RF03()
    {
        var evento = EventoConInicioEn(TimeSpan.FromHours(23) + TimeSpan.FromMinutes(59));

        var act = () => evento.Reservar(6, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RF-03");
    }

    [Fact]
    public void Reservar_a_mas_de_24h_no_aplica_limite_de_5()
    {
        var evento = EventoConInicioEn(TimeSpan.FromHours(24) + TimeSpan.FromMinutes(1), capacidad: 100);

        var act = () => evento.Reservar(6, "Ana", Dominio.Email(), _reloj);

        act.Should().NotThrow();
    }

    // ---- RN05: precio > $100 -> máximo 10 por transacción ----
    [Fact]
    public void Reservar_precio_alto_permite_hasta_10()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), precio: 150m, capacidad: 50);

        var act = () => evento.Reservar(10, "Ana", Dominio.Email(), _reloj);

        act.Should().NotThrow();
    }

    [Fact]
    public void Reservar_precio_alto_mas_de_10_lanza_RN05()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), precio: 150m, capacidad: 50);

        var act = () => evento.Reservar(11, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN05");
    }

    [Theory]
    [InlineData(100.0)]   // exactamente 100 -> NO es precio alto (estricto > 100)
    public void Reservar_precio_igual_a_100_no_aplica_limite_de_10(double precio)
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), precio: (decimal)precio, capacidad: 50);

        var act = () => evento.Reservar(11, "Ana", Dominio.Email(), _reloj);

        act.Should().NotThrow();
    }

    [Fact]
    public void Reservar_precio_apenas_mayor_a_100_aplica_limite_de_10()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), precio: 100.01m, capacidad: 50);

        var act = () => evento.Reservar(11, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN05");
    }

    // ---- A-02: composición de límites -> gana la más restrictiva ----
    [Fact]
    public void Reservar_evento_proximo_y_precio_alto_aplica_el_limite_mas_restrictivo_5()
    {
        // < 24h (límite 5) y precio > 100 (límite 10) -> min = 5, regla RF-03.
        var evento = EventoConInicioEn(TimeSpan.FromHours(10), precio: 150m, capacidad: 50);

        var act = () => evento.Reservar(6, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RF-03");
    }

    // ---- Aforo: no sobreventa ----
    [Fact]
    public void Reservar_mas_que_lo_disponible_lanza_CAPACIDAD()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), capacidad: 10);
        evento.Reservar(6, "Ana", Dominio.Email(), _reloj);
        evento.Reservar(4, "Beto", Dominio.Email("beto@correo.com"), _reloj); // ocupadas = 10

        var act = () => evento.Reservar(1, "Caro", Dominio.Email("caro@correo.com"), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("CAPACIDAD");
        evento.EntradasDisponibles.Should().Be(0);
    }

    [Fact]
    public void Reservar_el_ultimo_cupo_exacto_es_valido()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), capacidad: 10);
        evento.Reservar(9, "Ana", Dominio.Email(), _reloj);

        var act = () => evento.Reservar(1, "Beto", Dominio.Email("beto@correo.com"), _reloj);

        act.Should().NotThrow();
        evento.EntradasDisponibles.Should().Be(0);
    }

    [Fact]
    public void Cancelar_una_reserva_pendiente_libera_cupo_para_nueva_reserva()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5), capacidad: 10);
        var r1 = evento.Reservar(10, "Ana", Dominio.Email(), _reloj);
        evento.EntradasDisponibles.Should().Be(0);

        evento.CancelarReserva(r1.Id, _reloj); // libera

        evento.EntradasDisponibles.Should().Be(10);
        var act = () => evento.Reservar(3, "Beto", Dominio.Email("beto@correo.com"), _reloj);
        act.Should().NotThrow();
    }

    // ---- RN06: no se reserva sobre eventos no activos ----
    [Fact]
    public void Reservar_sobre_evento_ya_finalizado_lanza_RN06()
    {
        var evento = Dominio.Evento(_reloj, inicio: _reloj.Now.AddHours(2), duracion: TimeSpan.FromHours(1));
        _reloj.Avanzar(TimeSpan.FromHours(5)); // evento completado

        var act = () => evento.Reservar(1, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN06");
    }

    [Fact]
    public void Reservar_sobre_evento_cancelado_lanza_RN06()
    {
        var evento = EventoConInicioEn(TimeSpan.FromDays(5));
        evento.Cancelar(_reloj);

        var act = () => evento.Reservar(1, "Ana", Dominio.Email(), _reloj);

        act.Should().Throw<ReglaNegocioException>().Which.Regla.Should().Be("RN06");
    }
}
