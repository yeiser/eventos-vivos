namespace EventosVivos.Application.Reservas.Commands.CrearReserva;

/// <summary>RF-03: datos para reservar entradas de un evento.</summary>
public sealed record CrearReservaCommand(
    Guid EventoId,
    int Cantidad,
    string NombreComprador,
    string EmailComprador);
