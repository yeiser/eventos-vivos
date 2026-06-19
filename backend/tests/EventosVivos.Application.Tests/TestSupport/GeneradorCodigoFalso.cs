using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Tests.TestSupport;

internal sealed class GeneradorCodigoFalso : IReservationCodeGenerator
{
    private int _siguiente = 1;

    public CodigoReserva Generar() => CodigoReserva.DesdeNumero(_siguiente++ % 1_000_000);
}
