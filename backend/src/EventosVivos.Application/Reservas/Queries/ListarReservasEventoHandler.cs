using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Mapping;

namespace EventosVivos.Application.Reservas.Queries;

/// <summary>Lista las reservas de un evento (vista de organizador/administrador).</summary>
public sealed class ListarReservasEventoHandler
{
    private readonly IEventoRepository _eventos;

    public ListarReservasEventoHandler(IEventoRepository eventos) => _eventos = eventos;

    public async Task<IReadOnlyList<ReservaDto>> EjecutarAsync(Guid eventoId, CancellationToken cancellationToken = default)
    {
        var evento = await _eventos.ObtenerPorIdAsync(eventoId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("evento", eventoId);

        return evento.Reservas
            .OrderByDescending(r => r.FechaReserva)
            .Select(r => r.ToDto())
            .ToList();
    }
}
