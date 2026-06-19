using EventosVivos.Domain.Common;

namespace EventosVivos.Application.Common;

/// <summary>Bloque de trazabilidad incluido en las respuestas de detalle (§16.5).</summary>
public sealed record AuditoriaDto(
    string? CreadoPor,
    DateTimeOffset FechaCreacion,
    string? ModificadoPor,
    DateTimeOffset? FechaUltimaModificacion)
{
    public static AuditoriaDto Desde(IAuditableEntity entidad) => new(
        entidad.CreadoPor,
        entidad.FechaCreacion,
        entidad.ModificadoPor,
        entidad.FechaUltimaModificacion);
}
