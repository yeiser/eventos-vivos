using EventosVivos.Domain.Reservas;

namespace EventosVivos.Domain.Tests.TestSupport;

/// <summary>Generador determinista de códigos de reserva para pruebas (EV-000001, EV-000002, ...).</summary>
internal sealed class GeneradorCodigoFalso : IReservationCodeGenerator
{
    private int _siguiente;

    public GeneradorCodigoFalso(int inicial = 1) => _siguiente = inicial;

    public CodigoReserva Generar() => CodigoReserva.DesdeNumero(_siguiente++ % 1_000_000);
}
