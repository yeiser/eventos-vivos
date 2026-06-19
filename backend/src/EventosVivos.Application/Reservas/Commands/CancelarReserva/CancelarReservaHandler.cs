using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Mapping;
using EventosVivos.Domain.Common;

namespace EventosVivos.Application.Reservas.Commands.CancelarReserva;

/// <summary>
/// RF-05 + RN07: cancela una reserva. El dominio decide si libera el cupo (Cancelada) o lo marca como
/// perdido (Perdida) según la antelación, y rechaza estados terminales.
/// </summary>
public sealed class CancelarReservaHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelarReservaHandler(IEventoRepository eventos, IUnitOfWork unitOfWork, IClock clock)
    {
        _eventos = eventos;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ReservaDto> EjecutarAsync(CancelarReservaCommand command, CancellationToken cancellationToken = default)
    {
        var evento = await _eventos.ObtenerPorReservaAsync(command.ReservaId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("reserva", command.ReservaId);

        var reserva = evento.CancelarReserva(command.ReservaId, _clock);

        await _unitOfWork.GuardarCambiosAsync(cancellationToken);
        return reserva.ToDto();
    }
}
