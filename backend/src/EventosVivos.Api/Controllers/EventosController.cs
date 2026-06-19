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
    /// <summary>RF-01: crea un evento (solo administradores).</summary>
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

    /// <summary>RF-02: lista eventos con filtros y paginación.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventoResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventoResumenDto>>> Listar(
        [FromQuery] EventoFiltro filtro,
        [FromServices] ListarEventosHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(filtro, cancellationToken));

    /// <summary>Detalle de un evento.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventoDto>> Obtener(
        Guid id,
        [FromServices] ObtenerEventoHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>RF-06: reporte de ocupación de un evento (solo administradores).</summary>
    [HttpGet("{id:guid}/reporte")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ReporteOcupacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReporteOcupacionDto>> Reporte(
        Guid id,
        [FromServices] ReporteOcupacionHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>Lista las reservas de un evento (vista de organizador, solo administradores).</summary>
    [HttpGet("{id:guid}/reservas")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<ReservaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ReservaDto>>> ListarReservas(
        Guid id,
        [FromServices] ListarReservasEventoHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>RF-03: reserva entradas de un evento.</summary>
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
