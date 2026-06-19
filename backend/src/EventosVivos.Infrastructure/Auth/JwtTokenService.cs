using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Auth;
using EventosVivos.Domain.Usuarios;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventosVivos.Infrastructure.Auth;

/// <summary>Emite tokens JWT firmados con HS256 e incluye los claims de identidad y rol (ADR-007).</summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public TokenAcceso GenerarToken(Usuario usuario)
    {
        var expiraEn = DateTimeOffset.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.Email, usuario.Email.Valor),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiraEn.UtcDateTime,
            signingCredentials: credenciales);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenAcceso(jwt, expiraEn);
    }
}
