namespace EventosVivos.Domain.Reservas;

/// <summary>
/// Estado de una reserva. Máquina de estados en DISEÑO-ARQUITECTURA.md §6.2.
/// <list type="bullet">
///   <item><see cref="PendientePago"/>: creada, ocupa cupo (RF-03).</item>
///   <item><see cref="Confirmada"/>: pago confirmado, con código (RF-04).</item>
///   <item><see cref="Cancelada"/>: liberada, vuelve a estar disponible (RF-05).</item>
///   <item><see cref="Perdida"/>: cancelada con &lt;48h siendo confirmada; NO libera, solo reporte (RN07).</item>
/// </list>
/// </summary>
public enum EstadoReserva
{
    PendientePago = 1,
    Confirmada = 2,
    Cancelada = 3,
    Perdida = 4
}
