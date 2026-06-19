namespace EventosVivos.Api.Contracts;

/// <summary>Cuerpo de la solicitud para reservar entradas (el eventoId va en la ruta).</summary>
public sealed record CrearReservaRequest(int Cantidad, string NombreComprador, string EmailComprador);
