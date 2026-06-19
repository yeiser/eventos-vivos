using EventosVivos.Domain.Common;

namespace EventosVivos.Api.IntegrationTests.Soporte;

/// <summary>Reloj con instante fijo para construir agregados deterministas en pruebas de persistencia.</summary>
internal sealed class RelojFijo : IClock
{
    public DateTimeOffset Now { get; }

    public RelojFijo(DateTimeOffset now) => Now = now;
}
