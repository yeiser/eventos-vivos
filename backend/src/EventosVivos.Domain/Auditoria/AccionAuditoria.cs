namespace EventosVivos.Domain.Auditoria;

/// <summary>Tipo de operación registrada en el audit trail (§16.2).</summary>
public enum AccionAuditoria
{
    Crear = 1,
    Actualizar = 2,
    Eliminar = 3
}
