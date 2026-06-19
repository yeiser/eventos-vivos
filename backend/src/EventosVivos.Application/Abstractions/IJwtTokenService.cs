using EventosVivos.Application.Auth;
using EventosVivos.Domain.Usuarios;

namespace EventosVivos.Application.Abstractions;

/// <summary>Genera tokens JWT firmados para un usuario (ADR-007).</summary>
public interface IJwtTokenService
{
    TokenAcceso GenerarToken(Usuario usuario);
}
