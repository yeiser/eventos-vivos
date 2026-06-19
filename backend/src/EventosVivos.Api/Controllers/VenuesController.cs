using EventosVivos.Application.Venues.Dtos;
using EventosVivos.Application.Venues.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/v1/venues")]
public sealed class VenuesController : ControllerBase
{
    private readonly ListarVenuesHandler _listar;

    public VenuesController(ListarVenuesHandler listar) => _listar = listar;

    /// <summary>Lista los venues (recintos) de referencia.</summary>
    /// <remarks>Datos maestros para crear eventos y mostrar el nombre del recinto en la UI.</remarks>
    /// <response code="200">Listado de venues con su capacidad.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VenueDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VenueDto>>> Listar(CancellationToken cancellationToken) =>
        Ok(await _listar.EjecutarAsync(cancellationToken));
}
