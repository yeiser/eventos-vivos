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
    /// <summary>§16.4: historial de cambios (audit trail) con filtros. Solo administradores. Solo lectura.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Listar(
        [FromQuery] AuditoriaFiltro filtro,
        [FromServices] ListarAuditoriaHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(filtro, cancellationToken));
}
