using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos.Dtos;
using EventosVivos.Application.Eventos.Mapping;
using EventosVivos.Domain.Common;

namespace EventosVivos.Application.Eventos.Queries;

/// <summary>RF-02: listado de eventos con filtros y paginación.</summary>
public sealed class ListarEventosHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IClock _clock;

    public ListarEventosHandler(IEventoRepository eventos, IClock clock)
    {
        _eventos = eventos;
        _clock = clock;
    }

    public async Task<PagedResult<EventoResumenDto>> EjecutarAsync(
        EventoFiltro filtro, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _eventos.ListarAsync(filtro, cancellationToken);
        var dtos = items.Select(e => e.ToResumen(_clock)).ToList();

        return new PagedResult<EventoResumenDto>(
            dtos, filtro.PaginaNormalizada, filtro.TamanoNormalizado, total);
    }
}
