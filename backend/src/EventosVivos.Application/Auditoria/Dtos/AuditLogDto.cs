using EventosVivos.Domain.Auditoria;

namespace EventosVivos.Application.Auditoria.Dtos;

/// <summary>Registro del audit trail para la consulta de auditoría (§16.4).</summary>
public sealed record AuditLogDto(
    Guid Id,
    string Entidad,
    string EntidadId,
    AccionAuditoria Accion,
    string Usuario,
    DateTimeOffset Fecha,
    string? ValoresAnteriores,
    string? ValoresNuevos,
    string? CamposModificados,
    string? TraceId,
    string? IpOrigen);
