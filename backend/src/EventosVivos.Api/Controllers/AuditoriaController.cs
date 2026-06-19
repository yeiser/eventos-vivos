using EventosVivos.Application.Auditoria;
using EventosVivos.Application.Auditoria.Dtos;
using EventosVivos.Application.Auditoria.Queries;
using EventosVivos.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/v1/auditoria")]
[Authorize(Roles = "Admin")]
public sealed class AuditoriaController : ControllerBase
{
    /// <summary>Historial de cambios (audit trail) con filtros — solo lectura.</summary>
    /// <remarks>
    /// Registro **inmutable** de auditoría: quién hizo qué y cuándo, con los valores antes/después y
    /// *masking* de datos sensibles. Filtros por entidad, id, usuario, acción y rango de fechas.
    /// </remarks>
    /// <response code="200">Página del historial de auditoría.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Listar(
        [FromQuery] AuditoriaFiltro filtro,
        [FromServices] ListarAuditoriaHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(filtro, cancellationToken));
}
