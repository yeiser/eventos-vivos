using EventosVivos.Application.Reservas;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Abstractions;

public interface IReservaRepository
{
    Task<Reserva?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Búsqueda global paginada de reservas (incluye el título del evento de cada reserva).</summary>
    Task<(IReadOnlyList<ReservaResumenDto> Items, int Total)> BuscarAsync(
        ReservaFiltro filtro, CancellationToken cancellationToken = default);
}
