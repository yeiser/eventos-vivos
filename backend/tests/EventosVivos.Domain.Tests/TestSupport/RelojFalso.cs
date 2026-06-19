using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Tests.TestSupport;

/// <summary>Reloj controlable para pruebas deterministas de las reglas temporales.</summary>
internal sealed class RelojFalso : IClock
{
    public DateTimeOffset Now { get; set; }

    public RelojFalso(DateTimeOffset now) => Now = now;

    public void Avanzar(TimeSpan tiempo) => Now = Now.Add(tiempo);
}
