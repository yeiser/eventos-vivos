using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Eventos;
using EventosVivos.Domain.Eventos;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public sealed class EventoRepository : IEventoRepository
{
    private readonly EventosDbContext _db;

    public EventoRepository(EventosDbContext db) => _db = db;

    public Task<Evento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Eventos
            .Include(e => e.Reservas)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<Evento?> ObtenerParaReservaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Bloqueo pesimista de la fila del evento: las reservas concurrentes del mismo evento se
        // serializan, de modo que el cálculo de disponibilidad siempre ve el estado confirmado previo.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM eventos WHERE id = {id} FOR UPDATE", cancellationToken);

        return await _db.Eventos
            .Include(e => e.Reservas)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<Evento?> ObtenerPorReservaAsync(Guid reservaId, CancellationToken cancellationToken = default) =>
        _db.Eventos
            .Include(e => e.Reservas)
            .FirstOrDefaultAsync(e => e.Reservas.Any(r => r.Id == reservaId), cancellationToken);

    public Task<bool> ExisteSolapamientoAsync(
        int venueId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        Guid? excluirEventoId = null,
        CancellationToken cancellationToken = default) =>
        _db.Eventos
            .Where(e => e.VenueId == venueId && e.Estado == EstadoEvento.Activo)
            .Where(e => excluirEventoId == null || e.Id != excluirEventoId)
            // RN02: solapamiento estricto [Inicio, Fin) — el contacto en extremos no cuenta.
            .AnyAsync(e => e.FechaInicio < fin && inicio < e.FechaFin, cancellationToken);

    public async Task<(IReadOnlyList<Evento> Items, int Total)> ListarAsync(
        EventoFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Eventos.AsNoTracking().Include(e => e.Reservas).AsQueryable();

        if (filtro.Tipo is not null)
            query = query.Where(e => e.Tipo == filtro.Tipo);
        if (filtro.VenueId is not null)
            query = query.Where(e => e.VenueId == filtro.VenueId);
        if (filtro.Estado is not null)
            query = query.Where(e => e.Estado == filtro.Estado);
        if (filtro.FechaInicioDesde is not null)
            query = query.Where(e => e.FechaInicio >= filtro.FechaInicioDesde);
        if (filtro.FechaInicioHasta is not null)
            query = query.Where(e => e.FechaInicio <= filtro.FechaInicioHasta);
        if (!string.IsNullOrWhiteSpace(filtro.Titulo))
            // RF-02: búsqueda parcial case-insensitive con ILIKE (nativo de PostgreSQL).
            query = query.Where(e => EF.Functions.ILike(e.Titulo, $"%{filtro.Titulo}%"));

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.FechaInicio)
            .Skip((filtro.PaginaNormalizada - 1) * filtro.TamanoNormalizado)
            .Take(filtro.TamanoNormalizado)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AgregarAsync(Evento evento, CancellationToken cancellationToken = default) =>
        await _db.Eventos.AddAsync(evento, cancellationToken);
}
