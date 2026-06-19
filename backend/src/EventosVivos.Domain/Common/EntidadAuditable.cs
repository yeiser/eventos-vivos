namespace EventosVivos.Domain.Common;

/// <summary>
/// Base para entidades que requieren trazabilidad. Encapsula los campos de auditoría
/// con setters controlados; el interceptor de infraestructura los rellena (§16.1).
/// </summary>
public abstract class EntidadAuditable : IAuditableEntity
{
    public DateTimeOffset FechaCreacion { get; private set; }
    public string? CreadoPor { get; private set; }
    public DateTimeOffset? FechaUltimaModificacion { get; private set; }
    public string? ModificadoPor { get; private set; }

    public void EstablecerCreacion(DateTimeOffset fecha, string usuario)
    {
        FechaCreacion = fecha;
        CreadoPor = usuario;
    }

    public void EstablecerModificacion(DateTimeOffset fecha, string usuario)
    {
        FechaUltimaModificacion = fecha;
        ModificadoPor = usuario;
    }
}
