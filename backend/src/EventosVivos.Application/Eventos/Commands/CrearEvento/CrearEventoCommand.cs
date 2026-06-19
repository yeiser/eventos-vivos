using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Eventos.Commands.CrearEvento;

/// <summary>RF-01: datos para crear un evento.</summary>
public sealed record CrearEventoCommand(
    string Titulo,
    string Descripcion,
    int VenueId,
    int CapacidadMaxima,
    DateTimeOffset FechaInicio,
    DateTimeOffset FechaFin,
    decimal Precio,
    TipoEvento Tipo);
