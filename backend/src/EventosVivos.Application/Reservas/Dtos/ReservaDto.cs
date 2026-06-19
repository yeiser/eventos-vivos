using EventosVivos.Application.Common;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Reservas.Dtos;

/// <summary>Representación de una reserva para las respuestas de la API.</summary>
public sealed record ReservaDto(
    Guid Id,
    Guid EventoId,
    int Cantidad,
    string NombreComprador,
    string EmailComprador,
    EstadoReserva Estado,
    string? Codigo,
    DateTimeOffset FechaReserva,
    DateTimeOffset? FechaConfirmacion,
    DateTimeOffset? FechaCancelacion,
    AuditoriaDto Auditoria);
