using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Auditoria.Dtos;
using EventosVivos.Application.Common;

namespace EventosVivos.Application.Auditoria.Queries;

/// <summary>§16.4: lista el audit trail con filtros y paginación (solo lectura).</summary>
public sealed class ListarAuditoriaHandler
{
    private readonly IAuditLogRepository _auditoria;

    public ListarAuditoriaHandler(IAuditLogRepository auditoria) => _auditoria = auditoria;

    public async Task<PagedResult<AuditLogDto>> EjecutarAsync(
        AuditoriaFiltro filtro, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _auditoria.ListarAsync(filtro, cancellationToken);

        var dtos = items.Select(a => new AuditLogDto(
            a.Id, a.Entidad, a.EntidadId, a.Accion, a.Usuario, a.Fecha,
            a.ValoresAnteriores, a.ValoresNuevos, a.CamposModificados, a.TraceId, a.IpOrigen)).ToList();

        return new PagedResult<AuditLogDto>(dtos, filtro.PaginaNormalizada, filtro.TamanoNormalizado, total);
    }
}
