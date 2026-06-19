using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Auditoria;
using EventosVivos.Domain.Auditoria;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly EventosDbContext _db;

    public AuditLogRepository(EventosDbContext db) => _db = db;

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> ListarAsync(
        AuditoriaFiltro filtro, CancellationToken cancellationToken = default)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
            query = query.Where(a => a.Entidad == filtro.Entidad);
        if (!string.IsNullOrWhiteSpace(filtro.EntidadId))
            query = query.Where(a => a.EntidadId == filtro.EntidadId);
        if (!string.IsNullOrWhiteSpace(filtro.Usuario))
            query = query.Where(a => a.Usuario == filtro.Usuario);
        if (filtro.Accion is not null)
            query = query.Where(a => a.Accion == filtro.Accion);
        if (filtro.FechaDesde is not null)
            query = query.Where(a => a.Fecha >= filtro.FechaDesde);
        if (filtro.FechaHasta is not null)
            query = query.Where(a => a.Fecha <= filtro.FechaHasta);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.Fecha)
            .Skip((filtro.PaginaNormalizada - 1) * filtro.TamanoNormalizado)
            .Take(filtro.TamanoNormalizado)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
