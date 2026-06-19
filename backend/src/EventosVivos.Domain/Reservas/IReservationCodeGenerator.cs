namespace EventosVivos.Domain.Reservas;

/// <summary>
/// Genera códigos de reserva únicos con formato EV-NNNNNN (RF-04).
/// La implementación de infraestructura asegura la unicidad (índice único + reintento ante colisión).
/// </summary>
public interface IReservationCodeGenerator
{
    CodigoReserva Generar();
}
