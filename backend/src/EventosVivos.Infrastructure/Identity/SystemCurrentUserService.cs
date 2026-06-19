using EventosVivos.Application.Abstractions;

namespace EventosVivos.Infrastructure.Identity;

/// <summary>
/// Implementación por defecto del actor cuando no hay petición HTTP (migraciones, seed, jobs, pruebas
/// de infraestructura). La API registra <c>CurrentUserService</c> que la sustituye en tiempo de petición.
/// </summary>
public sealed class SystemCurrentUserService : ICurrentUserService
{
    public const string Sistema = "system";

    public string UsuarioId => Sistema;
    public string NombreUsuario => Sistema;
    public string? Rol => null;
    public string? IpOrigen => null;
    public bool EstaAutenticado => false;
}
