using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos.Dtos;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Eventos.Mapping;

/// <summary>Mapeo explícito de dominio a DTOs (sin AutoMapper). Usa el reloj para el estado efectivo (RN06).</summary>
public static class EventoMapper
{
    public static EventoDto ToDto(this Evento evento, IClock clock) => new(
        evento.Id,
        evento.Titulo,
        evento.Descripcion,
        evento.VenueId,
        evento.CapacidadMaxima,
        evento.FechaInicio,
        evento.FechaFin,
        evento.Precio,
        evento.Tipo,
        evento.EstadoEfectivo(clock),
        evento.EntradasConfirmadas,
        evento.EntradasDisponibles,
        AuditoriaDto.Desde(evento));

    public static EventoResumenDto ToResumen(this Evento evento, IClock clock) => new(
        evento.Id,
        evento.Titulo,
        evento.VenueId,
        evento.FechaInicio,
        evento.FechaFin,
        evento.Precio,
        evento.Tipo,
        evento.EstadoEfectivo(clock),
        evento.CapacidadMaxima,
        evento.EntradasDisponibles);

    public static ReporteOcupacionDto ToDto(this ReporteOcupacion reporte) => new(
        reporte.EventoId,
        reporte.Titulo,
        reporte.Estado,
        reporte.CapacidadMaxima,
        reporte.EntradasVendidas,
        reporte.EntradasDisponibles,
        reporte.PorcentajeOcupacion,
        reporte.IngresosTotales);
}
