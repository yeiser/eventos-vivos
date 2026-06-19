using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos.Dtos;
using EventosVivos.Application.Eventos.Mapping;
using EventosVivos.Domain.Common;

namespace EventosVivos.Application.Eventos.Queries;

/// <summary>Detalle de un evento por id.</summary>
public sealed class ObtenerEventoHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IClock _clock;

    public ObtenerEventoHandler(IEventoRepository eventos, IClock clock)
    {
        _eventos = eventos;
        _clock = clock;
    }

    public async Task<EventoDto> EjecutarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var evento = await _eventos.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new RecursoNoEncontradoException("evento", id);

        return evento.ToDto(_clock);
    }
}
