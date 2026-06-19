using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Venues;

namespace EventosVivos.Domain.Tests.TestSupport;

/// <summary>Builders y constantes para construir agregados válidos en las pruebas.</summary>
internal static class Dominio
{
    /// <summary>Instante base (lunes 2026-06-01 10:00, zona Colombia -05:00).</summary>
    public static readonly DateTimeOffset Ahora = new(2026, 6, 1, 10, 0, 0, TimeSpan.FromHours(-5));

    public static readonly TimeSpan OffsetCo = TimeSpan.FromHours(-5);

    public static RelojFalso Reloj(DateTimeOffset? now = null) => new(now ?? Ahora);

    public static Venue Venue(int capacidad = 200) => new(1, "Auditorio Central", capacidad, "Bogotá");

    public static Email Email(string valor = "comprador@correo.com") =>
        EventosVivos.Domain.Common.Email.Crear(valor);

    /// <summary>
    /// Crea un evento válido. Por defecto inicia 10 días después de <paramref name="clock"/> a las 10:00
    /// (día laboral, no nocturno) para no disparar RN03/RN04/RF-03 salvo que se indique <paramref name="inicio"/>.
    /// </summary>
    public static Evento Evento(
        IClock clock,
        Venue? venue = null,
        int capacidad = 100,
        decimal precio = 50m,
        DateTimeOffset? inicio = null,
        TimeSpan? duracion = null,
        TipoEvento tipo = TipoEvento.Conferencia,
        string titulo = "Concierto de prueba",
        string descripcion = "Descripción de prueba suficientemente larga.")
    {
        venue ??= Venue();
        var ini = inicio ?? clock.Now.AddDays(10);
        var fin = ini + (duracion ?? TimeSpan.FromHours(2));
        return EventosVivos.Domain.Eventos.Evento.Crear(
            titulo, descripcion, venue, capacidad, ini, fin, precio, tipo, clock);
    }
}
