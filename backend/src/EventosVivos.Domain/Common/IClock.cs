namespace EventosVivos.Domain.Common;

/// <summary>
/// Abstracción del reloj del sistema. Permite que las reglas sensibles al tiempo
/// (RN03, RN04, RN06, RN07, ventana de 24h de RF-03) sean deterministas y testeables.
/// </summary>
public interface IClock
{
    /// <summary>Instante actual con información de zona horaria (UTC-aware).</summary>
    DateTimeOffset Now { get; }
}
