using System.Security.Claims;
using EventosVivos.Application.Abstractions;

namespace EventosVivos.Api.Identity;

/// <summary>
/// Lee el actor actual de los claims del JWT (HttpContext). Sin petición autenticada devuelve "system",
/// que es el actor usado por migraciones/seed y por la auditoría cuando no hay usuario (§16.3).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    public const string Sistema = "system";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Usuario => _accessor.HttpContext?.User;

    public bool EstaAutenticado => Usuario?.Identity?.IsAuthenticated ?? false;

    public string UsuarioId =>
        Usuario?.FindFirstValue(ClaimTypes.NameIdentifier) is { Length: > 0 } id ? id : Sistema;

    public string NombreUsuario =>
        Usuario?.FindFirstValue(ClaimTypes.Name) is { Length: > 0 } nombre ? nombre : Sistema;

    public string? Rol => Usuario?.FindFirstValue(ClaimTypes.Role);

    public string? IpOrigen => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
