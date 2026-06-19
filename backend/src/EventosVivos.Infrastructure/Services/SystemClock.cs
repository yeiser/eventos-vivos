using EventosVivos.Domain.Common;

namespace EventosVivos.Infrastructure.Services;

/// <summary>Reloj real del sistema (UTC). En pruebas se sustituye por un reloj falso.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
