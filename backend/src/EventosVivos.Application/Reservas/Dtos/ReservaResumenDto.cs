using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Reservas.Dtos;

/// <summary>Resultado de la búsqueda de reservas (incluye el título del evento para ubicarla).</summary>
public sealed record ReservaResumenDto(
    Guid Id,
    Guid EventoId,
    string EventoTitulo,
    int Cantidad,
    string NombreComprador,
    string EmailComprador,
    EstadoReserva Estado,
    string? Codigo,
    DateTimeOffset FechaReserva);
