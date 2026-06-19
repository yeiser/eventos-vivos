using EventosVivos.Domain.Auditoria;

namespace EventosVivos.Application.Auditoria;

/// <summary>Filtros y paginación para consultar el audit trail (§16.4).</summary>
public sealed record AuditoriaFiltro
{
    public string? Entidad { get; init; }
    public string? EntidadId { get; init; }
    public string? Usuario { get; init; }
    public AccionAuditoria? Accion { get; init; }
    public DateTimeOffset? FechaDesde { get; init; }
    public DateTimeOffset? FechaHasta { get; init; }

    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 20;

    public int PaginaNormalizada => Pagina < 1 ? 1 : Pagina;
    public int TamanoNormalizado => TamanoPagina is < 1 or > 100 ? 20 : TamanoPagina;
}
