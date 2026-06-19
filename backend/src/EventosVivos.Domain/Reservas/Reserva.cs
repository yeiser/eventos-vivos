using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Reservas;

/// <summary>
/// Reserva de entradas para un evento. Forma parte del agregado <c>Evento</c> (raíz):
/// se crea mediante <c>Evento.Reservar(...)</c> para que el aforo quede protegido por la raíz.
/// Implementa la máquina de estados de RF-03/04/05 + RN07 (§6.2).
/// </summary>
public sealed class Reserva : EntidadAuditable
{
    /// <summary>RN07: ventana de penalización para cancelar una reserva confirmada.</summary>
    public static readonly TimeSpan VentanaPenalizacion = TimeSpan.FromHours(48);

    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public int Cantidad { get; private set; }
    public string NombreComprador { get; private set; } = null!;
    public Email EmailComprador { get; private set; } = null!;
    public EstadoReserva Estado { get; private set; }
    public CodigoReserva? Codigo { get; private set; }

    public DateTimeOffset FechaReserva { get; private set; }
    public DateTimeOffset? FechaConfirmacion { get; private set; }
    public DateTimeOffset? FechaCancelacion { get; private set; }

    /// <summary>
    /// Indica si la reserva consume capacidad del evento (§9). Las canceladas liberan cupo;
    /// las <see cref="EstadoReserva.Perdida"/> NO (RN07).
    /// </summary>
    public bool OcupaCapacidad =>
        Estado is EstadoReserva.PendientePago or EstadoReserva.Confirmada or EstadoReserva.Perdida;

    private Reserva() { } // EF

    private Reserva(Guid id, Guid eventoId, int cantidad, string nombreComprador, Email email, DateTimeOffset fechaReserva)
    {
        Id = id;
        EventoId = eventoId;
        Cantidad = cantidad;
        NombreComprador = nombreComprador;
        EmailComprador = email;
        FechaReserva = fechaReserva;
        Estado = EstadoReserva.PendientePago;
    }

    /// <summary>
    /// Crea una reserva en estado <see cref="EstadoReserva.PendientePago"/>. Solo se invoca desde
    /// la raíz del agregado (<c>Evento.Reservar</c>), que ya validó aforo y reglas de transacción.
    /// </summary>
    internal static Reserva Crear(Guid eventoId, int cantidad, string nombreComprador, Email email, DateTimeOffset ahora)
    {
        if (cantidad < 1)
            throw new DatosInvalidosException("La cantidad debe ser 1 o más.");
        if (string.IsNullOrWhiteSpace(nombreComprador))
            throw new DatosInvalidosException("El nombre del comprador es obligatorio.");
        ArgumentNullException.ThrowIfNull(email);

        return new Reserva(Guid.NewGuid(), eventoId, cantidad, nombreComprador.Trim(), email, ahora);
    }

    /// <summary>
    /// RF-04: confirma el pago. Solo desde <see cref="EstadoReserva.PendientePago"/>.
    /// Genera el código de reserva y marca la fecha de confirmación.
    /// </summary>
    public void ConfirmarPago(IReservationCodeGenerator generador, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(generador);

        if (Estado == EstadoReserva.Confirmada)
            throw new EstadoInvalidoException("La reserva ya está confirmada.");
        if (Estado is EstadoReserva.Cancelada or EstadoReserva.Perdida)
            throw new EstadoInvalidoException($"No se puede confirmar una reserva en estado {Estado}.");

        Estado = EstadoReserva.Confirmada;
        Codigo = generador.Generar();
        FechaConfirmacion = ahora;
    }

    /// <summary>
    /// RF-05 + RN07: cancela la reserva. Permitida desde <see cref="EstadoReserva.PendientePago"/>
    /// y <see cref="EstadoReserva.Confirmada"/>; rechazada en estados terminales (A-01).
    /// Si estaba confirmada y faltan menos de 48h para el evento, se marca como
    /// <see cref="EstadoReserva.Perdida"/> (no libera cupo, RN07); en otro caso, <see cref="EstadoReserva.Cancelada"/>.
    /// </summary>
    public void Cancelar(DateTimeOffset ahora, DateTimeOffset fechaInicioEvento)
    {
        if (Estado is EstadoReserva.Cancelada or EstadoReserva.Perdida)
            throw new EstadoInvalidoException($"La reserva ya está en un estado terminal ({Estado}).");

        FechaCancelacion = ahora;

        var faltaParaEvento = fechaInicioEvento - ahora;
        var esConfirmadaTardia = Estado == EstadoReserva.Confirmada && faltaParaEvento < VentanaPenalizacion;

        Estado = esConfirmadaTardia ? EstadoReserva.Perdida : EstadoReserva.Cancelada;
    }
}
