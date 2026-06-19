using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Mapping;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Reservas.Commands.ConfirmarPago;

/// <summary>RF-04: cambia la reserva a confirmada y le asigna un código único (EV-NNNNNN).</summary>
public sealed class ConfirmarPagoHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReservationCodeGenerator _generador;
    private readonly IClock _clock;

    public ConfirmarPagoHandler(
        IEventoRepository eventos,
        IUnitOfWork unitOfWork,
        IReservationCodeGenerator generador,
        IClock clock)
    {
        _eventos = eventos;
        _unitOfWork = unitOfWork;
        _generador = generador;
        _clock = clock;
    }

    public async Task<ReservaDto> EjecutarAsync(ConfirmarPagoCommand command, CancellationToken cancellationToken = default)
    {
        var evento = await _eventos.ObtenerPorReservaAsync(command.ReservaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("reserva", command.ReservaId);

        var reserva = evento.ConfirmarPagoReserva(command.ReservaId, _generador, _clock);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);
        return reserva.ToDto();
    }
}
