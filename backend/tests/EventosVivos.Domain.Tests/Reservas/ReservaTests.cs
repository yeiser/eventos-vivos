using EventosVivos.Domain.Common;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Domain.Tests.Reservas;

public class ReservaTests
{
    private static readonly DateTimeOffset Ahora = Dominio.Ahora;

    private static Reserva ReservaPendiente() =>
        Reserva.Crear(Guid.NewGuid(), 2, "Ana Pérez", Dominio.Email(), Ahora);

    private static Reserva ReservaConfirmada()
    {
        var r = ReservaPendiente();
        r.ConfirmarPago(new GeneradorCodigoFalso(), Ahora);
        return r;
    }

    // ---- RF-04: confirmar pago ----
    [Fact]
    public void ConfirmarPago_desde_pendiente_genera_codigo_y_fecha()
    {
        var reserva = ReservaPendiente();

        reserva.ConfirmarPago(new GeneradorCodigoFalso(123), Ahora);

        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.Codigo!.Valor.Should().MatchRegex(@"^EV-\d{6}$");
        reserva.FechaConfirmacion.Should().Be(Ahora);
    }

    [Fact]
    public void ConfirmarPago_de_una_reserva_ya_confirmada_lanza_EstadoInvalido()
    {
        var reserva = ReservaConfirmada();

        var act = () => reserva.ConfirmarPago(new GeneradorCodigoFalso(), Ahora);

        act.Should().Throw<EstadoInvalidoException>();
    }

    [Fact]
    public void ConfirmarPago_de_una_reserva_cancelada_lanza_EstadoInvalido()
    {
        var reserva = ReservaPendiente();
        reserva.Cancelar(Ahora, Ahora.AddDays(10)); // -> Cancelada

        var act = () => reserva.ConfirmarPago(new GeneradorCodigoFalso(), Ahora);

        act.Should().Throw<EstadoInvalidoException>();
    }

    // ---- RF-05: cancelar ----
    [Fact]
    public void Cancelar_una_reserva_pendiente_la_marca_cancelada_y_libera_cupo()
    {
        var reserva = ReservaPendiente();

        reserva.Cancelar(Ahora, Ahora.AddDays(10));

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.FechaCancelacion.Should().Be(Ahora);
        reserva.OcupaCapacidad.Should().BeFalse();
    }

    [Fact]
    public void Cancelar_una_reserva_confirmada_con_mas_de_48h_la_cancela_y_libera()
    {
        var reserva = ReservaConfirmada();

        reserva.Cancelar(Ahora, Ahora.AddHours(50)); // >= 48h

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.OcupaCapacidad.Should().BeFalse();
    }

    // ---- RN07: cancelar confirmada con < 48h -> perdida (no libera) ----
    [Fact]
    public void Cancelar_una_reserva_confirmada_con_menos_de_48h_la_marca_perdida()
    {
        var reserva = ReservaConfirmada();

        reserva.Cancelar(Ahora, Ahora.AddHours(47)); // < 48h

        reserva.Estado.Should().Be(EstadoReserva.Perdida);
        reserva.OcupaCapacidad.Should().BeTrue(); // no libera
    }

    [Theory]
    [InlineData(47, EstadoReserva.Perdida)]    // < 48h -> perdida
    [InlineData(48, EstadoReserva.Cancelada)]  // exactamente 48h -> NO penaliza (estricto)
    [InlineData(49, EstadoReserva.Cancelada)]  // > 48h -> cancelada
    public void Cancelar_confirmada_aplica_RN07_segun_la_ventana_de_48h(int horas, EstadoReserva esperado)
    {
        var reserva = ReservaConfirmada();

        reserva.Cancelar(Ahora, Ahora.AddHours(horas));

        reserva.Estado.Should().Be(esperado);
    }

    [Fact]
    public void Una_reserva_pendiente_cancelada_tarde_se_cancela_no_se_pierde()
    {
        // RN07 solo penaliza reservas CONFIRMADAS; una pendiente siempre se cancela y libera.
        var reserva = ReservaPendiente();

        reserva.Cancelar(Ahora, Ahora.AddHours(1)); // muy cerca del evento, pero pendiente

        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.OcupaCapacidad.Should().BeFalse();
    }

    [Fact]
    public void Cancelar_una_reserva_ya_cancelada_lanza_EstadoInvalido()
    {
        var reserva = ReservaPendiente();
        reserva.Cancelar(Ahora, Ahora.AddDays(10));

        var act = () => reserva.Cancelar(Ahora, Ahora.AddDays(10));

        act.Should().Throw<EstadoInvalidoException>();
    }

    [Fact]
    public void Cancelar_una_reserva_perdida_lanza_EstadoInvalido()
    {
        var reserva = ReservaConfirmada();
        reserva.Cancelar(Ahora, Ahora.AddHours(10)); // -> Perdida

        var act = () => reserva.Cancelar(Ahora, Ahora.AddHours(10));

        act.Should().Throw<EstadoInvalidoException>();
    }
}
