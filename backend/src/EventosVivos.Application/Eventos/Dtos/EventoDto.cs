using EventosVivos.Application.Common;
using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Eventos.Dtos;

/// <summary>Detalle de un evento. <see cref="Estado"/> es el estado efectivo (RN06).</summary>
public sealed record EventoDto(
    Guid Id,
    string Titulo,
    string Descripcion,
    int VenueId,
    int CapacidadMaxima,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    decimal Precio,
    TipoEvento Tipo,
    EstadoEvento Estado,
    int EntradasVendidas,
    int EntradasDisponibles,
    AuditoriaDto Auditoria);

/// <summary>Resumen de evento para listados (RF-02).</summary>
public sealed record EventoResumenDto(
    Guid Id,
    string Titulo,
    int VenueId,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    decimal Precio,
    TipoEvento Tipo,
    EstadoEvento Estado,
    int CapacidadMaxima,
    int EntradasDisponibles);
