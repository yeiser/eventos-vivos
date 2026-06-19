using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Mapping;

namespace EventosVivos.Application.Reservas.Queries;

/// <summary>Detalle de una reserva por id.</summary>
public sealed class ObtenerReservaHandler
{
    private readonly IReservaRepository _reservas;

    public ObtenerReservaHandler(IReservaRepository reservas) => _reservas = reservas;

    public async Task<ReservaDto> EjecutarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var reserva = await _reservas.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException("reserva", id);

        return reserva.ToDto();
    }
}
