using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Eventos;

/// <summary>
/// Value Object que representa el intervalo [Inicio, Fin) de un evento.
/// Garantiza Fin &gt; Inicio (RF-01) y permite detectar superposición de horarios (RN02).
/// Las fechas llevan la zona horaria del venue (ver §A-07).
/// </summary>
public sealed record PeriodoEvento
{
    public DateTimeOffset Inicio { get; }
    public DateTimeOffset Fin { get; }

    private PeriodoEvento(DateTimeOffset inicio, DateTimeOffset fin)
    {
        Inicio = inicio;
        Fin = fin;
    }

    public static PeriodoEvento Crear(DateTimeOffset inicio, DateTimeOffset fin)
    {
        if (fin <= inicio)
            throw new DatosInvalidosException("La fecha y hora de fin debe ser posterior a la de inicio.");

        return new PeriodoEvento(inicio, fin);
    }

    public TimeSpan Duracion => Fin - Inicio;

    /// <summary>
    /// RN02: dos intervalos se superponen si <c>Inicio &lt; otro.Fin</c> y <c>otro.Inicio &lt; Fin</c>.
    /// El contacto en los extremos (un evento termina justo cuando otro empieza) NO se considera superposición.
    /// </summary>
    public bool SeSuperponeCon(PeriodoEvento otro)
    {
        ArgumentNullException.ThrowIfNull(otro);
        return Inicio < otro.Fin && otro.Inicio < Fin;
    }
}
