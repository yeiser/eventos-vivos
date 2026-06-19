using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Venues;

namespace EventosVivos.Api.IntegrationTests.Soporte;

/// <summary>Constantes y builders para las pruebas de persistencia.</summary>
internal static class Datos
{
    /// <summary>Reloj fijo (antes de los eventos) para validar "fecha futura".</summary>
    public static readonly IClock Reloj = new RelojFijo(new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero));

    /// <summary>Instante base de los eventos: martes 14:00 UTC (no nocturno, no fin de semana).</summary>
    public static readonly DateTimeOffset Base = new(2026, 9, 15, 14, 0, 0, TimeSpan.Zero);

    public static Evento CrearEvento(
        int venueId = 1,
        int capacidad = 100,
        decimal precio = 50m,
        DateTimeOffset? inicio = null,
        TimeSpan? duracion = null,
        TipoEvento tipo = TipoEvento.Conferencia,
        string titulo = "Evento de prueba",
        string descripcion = "Descripción de prueba suficientemente larga.")
    {
        // Venue con capacidad amplia para no chocar con RN01 (el FK solo exige que el id exista: 1, 2 o 3).
        var venue = new Venue(venueId, "Venue", 500, "Bogotá");
        var ini = inicio ?? Base;
        var fin = ini + (duracion ?? TimeSpan.FromHours(2));
        return Evento.Crear(titulo, descripcion, venue, capacidad, ini, fin, precio, tipo, Reloj);
    }

    public static Email Email(string valor = "comprador@correo.com") => EventosVivos.Domain.Common.Email.Crear(valor);
}
