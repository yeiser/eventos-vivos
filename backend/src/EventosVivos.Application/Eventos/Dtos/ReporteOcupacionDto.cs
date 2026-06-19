using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Eventos.Dtos;

/// <summary>Reporte de ocupación de un evento (RF-06).</summary>
public sealed record ReporteOcupacionDto(
    Guid EventoId,
    string Titulo,
    EstadoEvento Estado,
    int CapacidadMaxima,
    int EntradasVendidas,
    int EntradasDisponibles,
    decimal PorcentajeOcupacion,
    decimal IngresosTotales);
