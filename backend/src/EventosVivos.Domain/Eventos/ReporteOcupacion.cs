namespace EventosVivos.Domain.Eventos;

/// <summary>
/// Resultado del reporte de ocupación de un evento (RF-06). Calculado por el dominio
/// a partir de las reservas y la capacidad (fórmulas en DISEÑO-ARQUITECTURA.md §9).
/// </summary>
public sealed record ReporteOcupacion(
    Guid EventoId,
    string Titulo,
    EstadoEvento Estado,
    int CapacidadMaxima,
    int EntradasVendidas,
    int EntradasDisponibles,
    decimal PorcentajeOcupacion,
    decimal IngresosTotales);
