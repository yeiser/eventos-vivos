using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Venues.Dtos;

namespace EventosVivos.Application.Venues.Queries;

public sealed class ListarVenuesHandler
{
    private readonly IVenueRepository _venues;

    public ListarVenuesHandler(IVenueRepository venues) => _venues = venues;

    public async Task<IReadOnlyList<VenueDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var venues = await _venues.ListarAsync(cancellationToken);
        return venues.Select(v => new VenueDto(v.Id, v.Nombre, v.Capacidad, v.Ciudad)).ToList();
    }
}
