using EventosVivos.Domain.Common;
using EventosVivos.Domain.Reservas;
using EventosVivos.Domain.Venues;

namespace EventosVivos.Domain.Eventos;

/// <summary>
/// Raíz del agregado. Protege la invariante de aforo (no sobreventa) y las reglas de creación
/// y reserva (RN01..RN06, RF-01/03/06). Las reservas se crean a través de <see cref="Reservar"/>.
/// </summary>
public sealed class Evento : EntidadAuditable
{
    // --- Límites del enunciado (RF-01 / RF-03 / RN03 / RN04 / RN05) ---
    public const int TituloMinimo = 5;
    public const int TituloMaximo = 100;
    public const int DescripcionMinima = 10;
    public const int DescripcionMaxima = 500;

    /// <summary>RN04: no se permiten reservas si faltan menos de esto para iniciar.</summary>
    public static readonly TimeSpan AntelacionMinimaReserva = TimeSpan.FromHours(1);
    /// <summary>RF-03: por debajo de esta antelación, máximo 5 entradas por transacción.</summary>
    public static readonly TimeSpan VentanaEventoProximo = TimeSpan.FromHours(24);
    public const int MaxEntradasEventoProximo = 5;       // RF-03
    public const decimal UmbralPrecioAlto = 100m;        // RN05 (estricto: > 100)
    public const int MaxEntradasPrecioAlto = 10;         // RN05
    /// <summary>RN03: hora límite de inicio en fin de semana (estricto: después de las 22:00).</summary>
    public static readonly TimeSpan HoraLimiteNocturnaFinDeSemana = new(22, 0, 0);

    private readonly List<Reserva> _reservas = new();

    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public int VenueId { get; private set; }
    public int CapacidadMaxima { get; private set; }
    public DateTimeOffset FechaInicio { get; private set; }
    public DateTimeOffset FechaFin { get; private set; }
    public decimal Precio { get; private set; }
    public TipoEvento Tipo { get; private set; }
    public EstadoEvento Estado { get; private set; }

    public IReadOnlyCollection<Reserva> Reservas => _reservas.AsReadOnly();

    /// <summary>Intervalo del evento como Value Object (computado, no se persiste por separado).</summary>
    public PeriodoEvento Periodo => PeriodoEvento.Crear(FechaInicio, FechaFin);

    // --- Aforo (§9) ---
    public int EntradasOcupadas => _reservas.Where(r => r.OcupaCapacidad).Sum(r => r.Cantidad);
    public int EntradasConfirmadas => _reservas
        .Where(r => r.Estado == EstadoReserva.Confirmada).Sum(r => r.Cantidad);
    public int EntradasDisponibles => CapacidadMaxima - EntradasOcupadas;

    private Evento() { } // EF

    private Evento(
        Guid id, string titulo, string descripcion, int venueId, int capacidadMaxima,
        PeriodoEvento periodo, decimal precio, TipoEvento tipo, EstadoEvento estado)
    {
        Id = id;
        Titulo = titulo;
        Descripcion = descripcion;
        VenueId = venueId;
        CapacidadMaxima = capacidadMaxima;
        FechaInicio = periodo.Inicio;
        FechaFin = periodo.Fin;
        Precio = precio;
        Tipo = tipo;
        Estado = estado;
    }

    /// <summary>
    /// RF-01: crea un evento aplicando todas las invariantes de creación y RN01 (capacidad ≤ venue),
    /// RN03 (horario nocturno en fin de semana) y "fecha futura".
    /// </summary>
    public static Evento Crear(
        string titulo,
        string descripcion,
        Venue venue,
        int capacidadMaxima,
        DateTimeOffset fechaInicio,
        DateTimeOffset fechaFin,
        decimal precio,
        TipoEvento tipo,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (venue is null)
            throw new DatosInvalidosException("El venue es obligatorio.");

        ValidarTitulo(titulo);
        ValidarDescripcion(descripcion);

        if (capacidadMaxima <= 0)
            throw new DatosInvalidosException("La capacidad máxima debe ser un entero positivo.");
        if (capacidadMaxima > venue.Capacidad)
            throw new ReglaNegocioException("RN01",
                $"La capacidad máxima ({capacidadMaxima}) no puede exceder la capacidad del venue ({venue.Capacidad}).");

        if (precio <= 0)
            throw new DatosInvalidosException("El precio de entrada debe ser un decimal positivo.");

        if (!Enum.IsDefined(tipo))
            throw new DatosInvalidosException("El tipo de evento no es válido.");

        var periodo = PeriodoEvento.Crear(fechaInicio, fechaFin); // valida fin > inicio (RF-01)

        if (fechaInicio <= clock.Now)
            throw new DatosInvalidosException("La fecha y hora de inicio debe ser futura.");

        ValidarHorarioNocturnoFinDeSemana(fechaInicio); // RN03

        return new Evento(Guid.NewGuid(), titulo.Trim(), descripcion.Trim(), venue.Id,
            capacidadMaxima, periodo, precio, tipo, EstadoEvento.Activo);
    }

    /// <summary>
    /// RN06: estado efectivo del evento. Un evento Activo cuyo fin ya pasó se considera Completado.
    /// Un evento Cancelado permanece Cancelado.
    /// </summary>
    public EstadoEvento EstadoEfectivo(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (Estado == EstadoEvento.Cancelado)
            return EstadoEvento.Cancelado;
        if (Estado == EstadoEvento.Completado)
            return EstadoEvento.Completado;

        return clock.Now > FechaFin ? EstadoEvento.Completado : EstadoEvento.Activo;
    }

    /// <summary>Cancela el evento (acción administrativa). Solo si está efectivamente Activo.</summary>
    public void Cancelar(IClock clock)
    {
        var efectivo = EstadoEfectivo(clock);
        if (efectivo != EstadoEvento.Activo)
            throw new EstadoInvalidoException($"No se puede cancelar un evento en estado {efectivo}.");

        Estado = EstadoEvento.Cancelado;
    }

    /// <summary>
    /// RF-03: crea una reserva validando aforo y reglas de transacción
    /// (RN04 reserva tardía, RF-03 ventana de 24h, RN05 precio alto, composición A-02).
    /// </summary>
    public Reserva Reservar(int cantidad, string nombreComprador, Email email, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(email);

        var ahora = clock.Now;

        if (EstadoEfectivo(clock) != EstadoEvento.Activo)
            throw new ReglaNegocioException("RN06",
                "No se pueden crear reservas para un evento que no está activo.");

        if (cantidad < 1)
            throw new DatosInvalidosException("La cantidad debe ser 1 o más.");
        if (string.IsNullOrWhiteSpace(nombreComprador))
            throw new DatosInvalidosException("El nombre del comprador es obligatorio.");

        var tiempoParaInicio = FechaInicio - ahora;

        // RN04: reserva tardía.
        if (tiempoParaInicio < AntelacionMinimaReserva)
            throw new ReglaNegocioException("RN04",
                "No se permiten reservas para eventos que inician en menos de 1 hora.");

        // RF-03 + RN05 compuestas: gana la regla más restrictiva (§A-02).
        var (limite, regla) = CalcularLimitePorTransaccion(tiempoParaInicio);
        if (cantidad > limite)
            throw new ReglaNegocioException(regla!,
                $"Para este evento solo se permiten {limite} entradas por transacción.");

        // Aforo (no sobreventa).
        if (cantidad > EntradasDisponibles)
            throw new ReglaNegocioException("CAPACIDAD",
                $"No hay suficientes entradas disponibles. Disponibles: {EntradasDisponibles}, solicitadas: {cantidad}.");

        var reserva = Reserva.Crear(Id, cantidad, nombreComprador, email, ahora);
        _reservas.Add(reserva);
        return reserva;
    }

    /// <summary>RF-04: confirma el pago de una reserva del evento.</summary>
    public Reserva ConfirmarPagoReserva(Guid reservaId, IReservationCodeGenerator generador, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var reserva = ObtenerReserva(reservaId);
        reserva.ConfirmarPago(generador, clock.Now);
        return reserva;
    }

    /// <summary>RF-05 + RN07: cancela una reserva del evento (libera cupo o la marca como perdida).</summary>
    public Reserva CancelarReserva(Guid reservaId, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var reserva = ObtenerReserva(reservaId);
        reserva.Cancelar(clock.Now, FechaInicio);
        return reserva;
    }

    /// <summary>RF-06: calcula el reporte de ocupación (fórmulas §9).</summary>
    public ReporteOcupacion CalcularOcupacion(IClock clock)
    {
        var confirmadas = EntradasConfirmadas;
        var porcentaje = CapacidadMaxima > 0
            ? Math.Round((decimal)confirmadas / CapacidadMaxima * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new ReporteOcupacion(
            Id,
            Titulo,
            EstadoEfectivo(clock),
            CapacidadMaxima,
            confirmadas,
            EntradasDisponibles,
            porcentaje,
            Precio * confirmadas);
    }

    private Reserva ObtenerReserva(Guid reservaId) =>
        _reservas.FirstOrDefault(r => r.Id == reservaId)
        ?? throw new DatosInvalidosException("La reserva no pertenece a este evento o no existe.");

    /// <summary>
    /// Composición de límites por transacción (§A-02): se aplican todas las reglas y gana la más
    /// restrictiva. Devuelve el límite efectivo y la regla que lo impone.
    /// </summary>
    private (int Limite, string? Regla) CalcularLimitePorTransaccion(TimeSpan tiempoParaInicio)
    {
        var limite = int.MaxValue;
        string? regla = null;

        if (tiempoParaInicio < VentanaEventoProximo)
        {
            limite = MaxEntradasEventoProximo; // RF-03
            regla = "RF-03";
        }

        if (Precio > UmbralPrecioAlto && MaxEntradasPrecioAlto < limite)
        {
            limite = MaxEntradasPrecioAlto; // RN05
            regla = "RN05";
        }

        return (limite, regla);
    }

    private static void ValidarTitulo(string? titulo)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DatosInvalidosException("El título es obligatorio.");
        var t = titulo.Trim();
        if (t.Length is < TituloMinimo or > TituloMaximo)
            throw new DatosInvalidosException($"El título debe tener entre {TituloMinimo} y {TituloMaximo} caracteres.");
    }

    private static void ValidarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new DatosInvalidosException("La descripción es obligatoria.");
        var d = descripcion.Trim();
        if (d.Length is < DescripcionMinima or > DescripcionMaxima)
            throw new DatosInvalidosException($"La descripción debe tener entre {DescripcionMinima} y {DescripcionMaxima} caracteres.");
    }

    /// <summary>RN03: eventos en fin de semana no pueden iniciar después de las 22:00 (hora local del venue).</summary>
    private static void ValidarHorarioNocturnoFinDeSemana(DateTimeOffset inicio)
    {
        var esFinDeSemana = inicio.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        if (esFinDeSemana && inicio.TimeOfDay > HoraLimiteNocturnaFinDeSemana)
            throw new ReglaNegocioException("RN03",
                "Los eventos en fin de semana (sábado/domingo) no pueden iniciar después de las 22:00.");
    }
}
