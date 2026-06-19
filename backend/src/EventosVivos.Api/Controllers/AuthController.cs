using EventosVivos.Application.Auth.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Autentica un usuario y devuelve un JWT.</summary>
    /// <remarks>
    /// Endpoint **público** (no requiere token): es el punto de entrada para obtener el JWT que luego se
    /// pega en **Authorize** 🔒. Tras **5** intentos fallidos la cuenta se bloquea **15 minutos**
    /// (HTTP 423) como remediación de fuerza bruta. Credenciales de demo: `admin` / `Admin123!`.
    /// </remarks>
    /// <response code="200">Credenciales válidas: token JWT, su expiración, el usuario y el rol.</response>
    /// <response code="400">Petición mal formada (faltan `nombreUsuario` o `password`).</response>
    /// <response code="401">Usuario o contraseña incorrectos.</response>
    /// <response code="423">Cuenta bloqueada temporalmente por intentos fallidos.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        [FromServices] LoginHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.EjecutarAsync(command, cancellationToken));
}
