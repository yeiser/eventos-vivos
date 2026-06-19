using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos.Dtos;
using EventosVivos.Application.Eventos.Mapping;
using EventosVivos.Domain.Common;

namespace EventosVivos.Application.Eventos.Queries;

/// <summary>RF-06: reporte de ocupación de un evento.</summary>
public sealed class ReporteOcupacionHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IClock _clock;

    public ReporteOcupacionHandler(IEventoRepository eventos, IClock clock)
    {
        _eventos = eventos;
        _clock = clock;
    }

    public async Task<ReporteOcupacionDto> EjecutarAsync(Guid eventoId, CancellationToken cancellationToken = default)
    {
        var evento = await _eventos.ObtenerPorIdAsync(eventoId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("evento", eventoId);

        return evento.CalcularOcupacion(_clock).ToDto();
    }
}
