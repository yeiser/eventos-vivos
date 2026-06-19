using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Venues;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public sealed class VenueRepository : IVenueRepository
{
    private readonly EventosDbContext _db;

    public VenueRepository(EventosDbContext db) => _db = db;

    public Task<Venue?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Venue>> ListarAsync(CancellationToken cancellationToken = default) =>
        await _db.Venues.AsNoTracking().OrderBy(v => v.Id).ToListAsync(cancellationToken);
}
