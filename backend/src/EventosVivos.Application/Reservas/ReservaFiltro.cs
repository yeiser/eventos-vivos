using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Reservas;

/// <summary>Filtros para la búsqueda global de reservas (por código, comprador, email, estado).</summary>
public sealed record ReservaFiltro
{
    public string? Codigo { get; init; }
    public string? NombreComprador { get; init; }
    public EstadoReserva? Estado { get; init; }

    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 20;

    public int PaginaNormalizada => Pagina < 1 ? 1 : Pagina;
    public int TamanoNormalizado => TamanoPagina is < 1 or > 100 ? 20 : TamanoPagina;
}
