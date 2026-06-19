using EventosVivos.Application.Eventos.Commands.CrearEvento;
using EventosVivos.Application.Reservas.Commands.CrearReserva;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Venues;

namespace EventosVivos.Application.Tests.TestSupport;

internal static class Datos
{
    public static readonly DateTimeOffset Ahora = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    public static RelojFijo Reloj() => new(Ahora);

    public static Venue Venue(int capacidad = 200) => new(1, "Auditorio Central", capacidad, "Bogotá");

    public static Evento Evento(IClock clock, int capacidad = 100, decimal precio = 50m, DateTimeOffset? inicio = null)
    {
        var ini = inicio ?? clock.Now.AddDays(10);
        return EventosVivos.Domain.Eventos.Evento.Crear(
            "Concierto de prueba", "Descripción válida y suficientemente larga.",
            Venue(500), capacidad, ini, ini.AddHours(2), precio, TipoEvento.Concierto, clock);
    }

    public static CrearEventoCommand CrearEventoCmd(
        int venueId = 1,
        int capacidad = 100,
        decimal precio = 50m,
        string titulo = "Concierto de prueba",
        string descripcion = "Descripción válida y suficientemente larga.",
        TipoEvento tipo = TipoEvento.Concierto,
        DateTimeOffset? inicio = null)
    {
        var ini = inicio ?? Ahora.AddDays(10);
        return new CrearEventoCommand(titulo, descripcion, venueId, capacidad, ini, ini.AddHours(2), precio, tipo);
    }

    public static CrearReservaCommand CrearReservaCmd(
        Guid eventoId,
        int cantidad = 2,
        string nombre = "Ana Pérez",
        string email = "ana@correo.com") =>
        new(eventoId, cantidad, nombre, email);
}
