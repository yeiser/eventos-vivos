using EventosVivos.Domain.Common;

namespace EventosVivos.Application.Tests.TestSupport;

internal sealed class RelojFijo : IClock
{
    public DateTimeOffset Now { get; set; }

    public RelojFijo(DateTimeOffset now) => Now = now;
}
