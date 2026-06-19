using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;

namespace EventosVivos.Application.Reservas.Queries;

/// <summary>Búsqueda global de reservas por código, comprador o estado (vista de administrador).</summary>
public sealed class BuscarReservasHandler
{
    private readonly IReservaRepository _reservas;

    public BuscarReservasHandler(IReservaRepository reservas) => _reservas = reservas;

    public async Task<PagedResult<ReservaResumenDto>> EjecutarAsync(
        ReservaFiltro filtro, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _reservas.BuscarAsync(filtro, cancellationToken);
        return new PagedResult<ReservaResumenDto>(items, filtro.PaginaNormalizada, filtro.TamanoNormalizado, total);
    }
}
