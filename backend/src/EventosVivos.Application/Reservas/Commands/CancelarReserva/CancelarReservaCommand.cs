namespace EventosVivos.Application.Reservas.Commands.CancelarReserva;

/// <summary>RF-05: cancelar una reserva.</summary>
public sealed record CancelarReservaCommand(Guid ReservaId);
