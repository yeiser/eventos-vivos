using EventosVivos.Domain.Venues;

namespace EventosVivos.Application.Abstractions;

public interface IVenueRepository
{
    Task<Venue?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Venue>> ListarAsync(CancellationToken cancellationToken = default);
}
