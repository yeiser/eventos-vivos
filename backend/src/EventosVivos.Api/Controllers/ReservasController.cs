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
    /// <summary>Búsqueda global de reservas por código, comprador o estado.</summary>
    /// <remarks>
    /// Útil cuando no se sabe a qué evento pertenece una reserva. El código se busca exacto; el nombre
    /// del comprador, de forma parcial (`ILIKE`). Devuelve resultados paginados.
    /// </remarks>
    /// <response code="200">Página de reservas que cumplen los filtros.</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<ReservaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReservaResumenDto>>> Buscar(
        [FromQuery] ReservaFiltro filtro,
        [FromServices] BuscarReservasHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(filtro, cancellationToken));

    /// <summary>Obtiene el detalle de una reserva por su id.</summary>
    /// <response code="200">Reserva encontrada (estado, código, fechas, comprador).</response>
    /// <response code="404">No existe una reserva con ese id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservaDto>> Obtener(
        Guid id,
        [FromServices] ObtenerReservaHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(id, cancellationToken));

    /// <summary>Confirma el pago de una reserva (RF-04).</summary>
    /// <remarks>Pasa la reserva de *pendiente de pago* a **confirmada** y le asigna su código.</remarks>
    /// <response code="200">Reserva confirmada (incluye el código generado).</response>
    /// <response code="404">No existe una reserva con ese id.</response>
    /// <response code="409">La reserva no está en un estado que permita confirmarla.</response>
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

    /// <summary>Cancela una reserva (RF-05).</summary>
    /// <remarks>
    /// Se cancela desde *pendiente de pago* o *confirmada*; cancelar una confirmada libera el cupo y
    /// aplica la penalización de RN07. Sobre estados terminales se rechaza (409).
    /// </remarks>
    /// <response code="200">Reserva cancelada.</response>
    /// <response code="404">No existe una reserva con ese id.</response>
    /// <response code="409">La reserva está en un estado terminal y no puede cancelarse.</response>
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
