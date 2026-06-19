using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Commands.CancelarReserva;
using EventosVivos.Application.Reservas.Commands.ConfirmarPago;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Tests.Reservas;

public class ConfirmarYCancelarHandlerTests
{
    private readonly IEventoRepository _eventos = Substitute.For<IEventoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RelojFijo _clock = Datos.Reloj();

    private (Evento evento, Reserva reserva) EventoConReserva()
    {
        var evento = Datos.Evento(_clock, capacidad: 100);
        var reserva = evento.Reservar(2, "Ana", EventosVivos.Domain.Common.Email.Crear("ana@correo.com"), _clock);
        return (evento, reserva);
    }

    // ---- RF-04 ----
    [Fact]
    public async Task ConfirmarPago_confirma_y_asigna_codigo()
    {
        var (evento, reserva) = EventoConReserva();
        _eventos.ObtenerPorReservaAsync(reserva.Id, Arg.Any<CancellationToken>()).Returns(evento);
        var handler = new ConfirmarPagoHandler(_eventos, _uow, new GeneradorCodigoFalso(), _clock);

        var dto = await handler.EjecutarAsync(new ConfirmarPagoCommand(reserva.Id));

        dto.Estado.Should().Be(EstadoReserva.Confirmada);
        dto.Codigo.Should().MatchRegex(@"^EV-\d{6}$");
        await _uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmarPago_lanza_NotFound_si_no_existe_la_reserva()
    {
        _eventos.ObtenerPorReservaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Evento?)null);
        var handler = new ConfirmarPagoHandler(_eventos, _uow, new GeneradorCodigoFalso(), _clock);

        var act = () => handler.EjecutarAsync(new ConfirmarPagoCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task ConfirmarPago_dos_veces_lanza_EstadoInvalido()
    {
        var (evento, reserva) = EventoConReserva();
        reserva.ConfirmarPago(new GeneradorCodigoFalso(), _clock.Now); // ya confirmada
        _eventos.ObtenerPorReservaAsync(reserva.Id, Arg.Any<CancellationToken>()).Returns(evento);
        var handler = new ConfirmarPagoHandler(_eventos, _uow, new GeneradorCodigoFalso(), _clock);

        var act = () => handler.EjecutarAsync(new ConfirmarPagoCommand(reserva.Id));

        await act.Should().ThrowAsync<EstadoInvalidoException>();
    }

    // ---- RF-05 ----
    [Fact]
    public async Task Cancelar_una_reserva_pendiente_la_cancela_y_libera()
    {
        var (evento, reserva) = EventoConReserva();
        _eventos.ObtenerPorReservaAsync(reserva.Id, Arg.Any<CancellationToken>()).Returns(evento);
        var handler = new CancelarReservaHandler(_eventos, _uow, _clock);

        var dto = await handler.EjecutarAsync(new CancelarReservaCommand(reserva.Id));

        dto.Estado.Should().Be(EstadoReserva.Cancelada);
        evento.EntradasOcupadas.Should().Be(0);
        await _uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancelar_lanza_NotFound_si_no_existe_la_reserva()
    {
        _eventos.ObtenerPorReservaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Evento?)null);
        var handler = new CancelarReservaHandler(_eventos, _uow, _clock);

        var act = () => handler.EjecutarAsync(new CancelarReservaCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<RecursoNoEncontradoException>();
    }
}
