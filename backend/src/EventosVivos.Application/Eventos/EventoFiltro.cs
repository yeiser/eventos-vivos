using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Eventos;

/// <summary>Filtros opcionales y paginación para el listado de eventos (RF-02).</summary>
public sealed record EventoFiltro
{
    public TipoEvento? Tipo { get; init; }
    public int? VenueId { get; init; }
    public EstadoEvento? Estado { get; init; }
    public DateTimeOffset? FechaInicioDesde { get; init; }
    public DateTimeOffset? FechaInicioHasta { get; init; }
    public string? Titulo { get; init; }

    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 20;

    public int PaginaNormalizada => Pagina < 1 ? 1 : Pagina;
    public int TamanoNormalizado => TamanoPagina is < 1 or > 100 ? 20 : TamanoPagina;
}
