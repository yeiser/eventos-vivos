using EventosVivos.Domain.Reservas;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Services;

/// <summary>
/// Genera códigos de reserva EV-NNNNNN únicos. Reintenta ante colisión consultando la BD;
/// el índice único de <c>reservas.codigo</c> es la garantía final de unicidad (RF-04).
/// </summary>
public sealed class GeneradorCodigoReserva : IReservationCodeGenerator
{
    private const int MaxIntentos = 15;
    private readonly EventosDbContext _db;

    public GeneradorCodigoReserva(EventosDbContext db) => _db = db;

    public CodigoReserva Generar()
    {
        for (var intento = 0; intento < MaxIntentos; intento++)
        {
            var codigo = CodigoReserva.DesdeNumero(Random.Shared.Next(0, 1_000_000));
            var existe = _db.Reservas.Any(r => r.Codigo == codigo);
            if (!existe)
                return codigo;
        }

        throw new InvalidOperationException("No se pudo generar un código de reserva único tras varios intentos.");
    }
}
