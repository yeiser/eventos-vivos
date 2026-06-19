namespace EventosVivos.Application.Common;

/// <summary>Resultado paginado genérico para listados (RF-02).</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int TamanoPagina,
    int Total)
{
    public int TotalPaginas => TamanoPagina > 0 ? (int)Math.Ceiling(Total / (double)TamanoPagina) : 0;
}
