namespace EventosVivos.Domain.Auditoria;

/// <summary>
/// Registro inmutable (append-only) del audit trail (§16.2). Lo genera un interceptor de
/// EF Core (Fase 6) a partir del ChangeTracker; aquí solo se modela la entidad.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public string Entidad { get; private set; } = null!;
    public string EntidadId { get; private set; } = null!;
    public AccionAuditoria Accion { get; private set; }
    public string Usuario { get; private set; } = null!;
    public DateTimeOffset Fecha { get; private set; }
    public string? ValoresAnteriores { get; private set; }
    public string? ValoresNuevos { get; private set; }
    public string? CamposModificados { get; private set; }
    public string? TraceId { get; private set; }
    public string? IpOrigen { get; private set; }

    private AuditLog() { } // EF

    public AuditLog(
        Guid id,
        string entidad,
        string entidadId,
        AccionAuditoria accion,
        string usuario,
        DateTimeOffset fecha,
        string? valoresAnteriores = null,
        string? valoresNuevos = null,
        string? camposModificados = null,
        string? traceId = null,
        string? ipOrigen = null)
    {
        Id = id;
        Entidad = entidad;
        EntidadId = entidadId;
        Accion = accion;
        Usuario = usuario;
        Fecha = fecha;
        ValoresAnteriores = valoresAnteriores;
        ValoresNuevos = valoresNuevos;
        CamposModificados = camposModificados;
        TraceId = traceId;
        IpOrigen = ipOrigen;
    }
}
