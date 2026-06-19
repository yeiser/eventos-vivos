namespace EventosVivos.Application.Reservas.Commands.ConfirmarPago;

/// <summary>RF-04: confirmar el pago de una reserva (acción de administrador).</summary>
public sealed record ConfirmarPagoCommand(Guid ReservaId);
