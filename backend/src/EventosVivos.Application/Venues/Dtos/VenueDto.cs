namespace EventosVivos.Application.Venues.Dtos;

/// <summary>Venue de referencia (apoyo a RF-01).</summary>
public sealed record VenueDto(int Id, string Nombre, int Capacidad, string Ciudad);
