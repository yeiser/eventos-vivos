namespace EventosVivos.Domain.Common;

/// <summary>
/// Marca una entidad con campos de trazabilidad (quién creó/modificó y cuándo).
/// Los valores NO los gestiona la lógica de negocio: los rellena un interceptor de
/// infraestructura (ver DISEÑO-ARQUITECTURA.md §16.1) a partir de ICurrentUserService.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset FechaCreacion { get; }
    string? CreadoPor { get; }
    DateTimeOffset? FechaUltimaModificacion { get; }
    string? ModificadoPor { get; }

    /// <summary>Establece los datos de auditoría de creación (lo invoca el interceptor).</summary>
    void EstablecerCreacion(DateTimeOffset fecha, string usuario);

    /// <summary>Registra una modificación (lo invoca el interceptor).</summary>
    void EstablecerModificacion(DateTimeOffset fecha, string usuario);
}
