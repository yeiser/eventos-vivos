using EventosVivos.Api.Contracts;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos;
using EventosVivos.Application.Eventos.Commands.CrearEvento;
using EventosVivos.Application.Eventos.Dtos;
using EventosVivos.Application.Eventos.Queries;
using EventosVivos.Application.Reservas.Commands.CrearReserva;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/v1/eventos")]
public sealed class EventosController : ControllerBase
{
    /// <summary>Crea un evento (RF-01).</summary>
    /// <remarks>
    /// Valida las reglas de creación: capacidad ≤ aforo del venue (RN01), sin solapamiento de horario en
    /// el mismo venue (RN02) y dentro del horario permitido (RN03 — noche / fin de semana).
    /// </remarks>
    /// <response code="201">Evento creado; la cabecera `Location` apunta a su detalle.</response>
    /// <response code="400">Datos de entrada inválidos.</response>
    /// <response code="422">Violación de una regla de negocio (solapamiento, capacidad, horario).</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(EventoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EventoDto>> Crear(
        [FromBody] CrearEventoCommand command,
        [FromServices] CrearEventoHandler handler,
        CancellationToken cancellationToken)
    {
        var evento = await handler.EjecutarAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Obtener), new { id = evento.Id }, evento);
    }

    /// <summary>Lista eventos con filtros y paginación (RF-02).</summary>
    /// <remarks>
    /// Filtros opcionales: título (búsqueda parcial vía `ILIKE`), tipo, estado, venue y rango de fechas.
    /// </remarks>
    /// <response code="200">Página de eventos con el total para paginar.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventoResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventoResumenDto>>> Listar(
        [FromQuery] EventoFiltro filtro,
        [FromServices] ListarEventosHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(filtro, cancellationToken));

    /// <summary>Obtiene el detalle de un evento por su id.</summary>
    /// <response code="200">Evento encontrado (incluye aforo y entradas disponibles).</response>
    /// <response code="404">No existe un evento con ese id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventoDto>> Obtener(
        Guid id,
        [FromServices] ObtenerEventoHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>Reporte de ocupación de un evento (RF-06).</summary>
    /// <remarks>Para el porcentaje de ocupación cuenta únicamente las reservas **confirmadas**.</remarks>
    /// <response code="200">Reporte con capacidad, vendidas, disponibles y % de ocupación.</response>
    /// <response code="404">No existe un evento con ese id.</response>
    [HttpGet("{id:guid}/reporte")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ReporteOcupacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReporteOcupacionDto>> Reporte(
        Guid id,
        [FromServices] ReporteOcupacionHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>Lista las reservas de un evento (vista de organizador).</summary>
    /// <response code="200">Reservas del evento, con su estado y código.</response>
    /// <response code="404">No existe un evento con ese id.</response>
    [HttpGet("{id:guid}/reservas")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReservaDto>>> ListarReservas(
        Guid id,
        [FromServices] ListarReservasEventoHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>Reserva entradas de un evento (RF-03).</summary>
    /// <remarks>
    /// Protegida contra **sobreventa**: corre en una transacción con bloqueo del evento. El límite de
    /// entradas por transacción combina RN04 (&lt; 1 h: no se permite), RF-03 (&lt; 24 h: máx. 5) y
    /// RN05 (precio &gt; 100: máx. 10), tomando el más restrictivo. Aplica *rate limiting* por IP.
    /// </remarks>
    /// <response code="201">Reserva creada en estado pendiente de pago; `Location` al detalle.</response>
    /// <response code="400">Datos inválidos (cantidad, comprador o email).</response>
    /// <response code="404">No existe el evento.</response>
    /// <response code="422">Regla de negocio: sin cupo, fuera de plazo o sobre el límite por transacción.</response>
    [HttpPost("{id:guid}/reservas")]
    [EnableRateLimiting("reservas")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservaDto>> Reservar(
        Guid id,
        [FromBody] CrearReservaRequest request,
        [FromServices] CrearReservaHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CrearReservaCommand(id, request.Cantidad, request.NombreComprador, request.EmailComprador);
        var reserva = await handler.EjecutarAsync(command, cancellationToken);
        return CreatedAtAction(nameof(ReservasController.Obtener), "Reservas", new { id = reserva.Id }, reserva);
    }
}
