using EventosVivos.Application.Auditoria;
using EventosVivos.Domain.Auditoria;

namespace EventosVivos.Application.Abstractions;

public interface IAuditLogRepository
{
    /// <summary>Listado paginado del audit trail (solo lectura; ordenado por fecha descendente).</summary>
    Task<(IReadOnlyList<AuditLog> Items, int Total)> ListarAsync(
        AuditoriaFiltro filtro, CancellationToken cancellationToken = default);
}
