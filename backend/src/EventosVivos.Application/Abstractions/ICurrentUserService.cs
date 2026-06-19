namespace EventosVivos.Application.Abstractions;

/// <summary>
/// Expone el actor de la petición actual (a partir del JWT). Es la única fuente del "quién" tanto
/// para autorización como para la auditoría (§16.3). Para operaciones del sistema devuelve "system".
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Identificador del usuario (claim sub) o "system".</summary>
    string UsuarioId { get; }

    /// <summary>Nombre de usuario (claim name) o "system".</summary>
    string NombreUsuario { get; }

    /// <summary>Rol del actor, o null si no autenticado.</summary>
    string? Rol { get; }

    /// <summary>IP de origen de la petición, si está disponible (para el audit trail).</summary>
    string? IpOrigen { get; }

    bool EstaAutenticado { get; }
}
