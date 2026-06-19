using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas;
using EventosVivos.Application.Reservas.Commands.CancelarReserva;
using EventosVivos.Application.Reservas.Commands.ConfirmarPago;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/v1/reservas")]
public sealed class ReservasController : ControllerBase
{
    /// <summary>Búsqueda global de reservas por código, comprador o estado (solo administradores).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<ReservaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReservaResumenDto>>> Buscar(
        [FromQuery] ReservaFiltro filtro,
        [FromServices] BuscarReservasHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(filtro, cancellationToken));

    /// <summary>Detalle de una reserva.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservaDto>> Obtener(
        Guid id,
        [FromServices] ObtenerReservaHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>RF-04: confirma el pago de una reserva (solo administradores).</summary>
    [HttpPost("{id:guid}/confirmacion")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaDto>> Confirmar(
        Guid id,
        [FromServices] ConfirmarPagoHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(new ConfirmarPagoCommand(id), cancellationToken));

    /// <summary>RF-05: cancela una reserva.</summary>
    [HttpPost("{id:guid}/cancelacion")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservaDto>> Cancelar(
        Guid id,
        [FromServices] CancelarReservaHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(new CancelarReservaCommand(id), cancellationToken));
}
