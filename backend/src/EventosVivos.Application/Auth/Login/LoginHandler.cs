using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Domain.Common;
using FluentValidation;

namespace EventosVivos.Application.Auth.Login;

/// <summary>
/// ADR-007: autentica un usuario y emite un JWT. Aplica protección anti-fuerza-bruta:
/// cuenta los intentos fallidos y bloquea la cuenta temporalmente al superar el umbral.
/// Credenciales inválidas → 401; cuenta bloqueada → 423.
/// </summary>
public sealed class LoginHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<LoginCommand> _validator;

    public LoginHandler(
        IUsuarioRepository usuarios,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<LoginCommand> validator)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _jwt = jwt;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<LoginResponse> EjecutarAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var usuario = await _usuarios.ObtenerPorNombreUsuarioAsync(command.NombreUsuario, cancellationToken);

        // Usuario inexistente: mismo error genérico (no se revela si el usuario existe).
        if (usuario is null)
            throw new CredencialesInvalidasException();

        // Cuenta ya bloqueada: ni siquiera se verifica la contraseña.
        if (usuario.EstaBloqueado(_clock))
            throw new CuentaBloqueadaException(usuario.BloqueadoHasta!.Value);

        if (!_hasher.Verificar(command.Password, usuario.PasswordHash))
        {
            usuario.RegistrarIntentoFallido(_clock);
            await _unitOfWork.GuardarCambiosAsync(cancellationToken);

            // Si este intento acaba de bloquear la cuenta, infórmalo; si no, error genérico.
            if (usuario.EstaBloqueado(_clock))
                throw new CuentaBloqueadaException(usuario.BloqueadoHasta!.Value);

            throw new CredencialesInvalidasException();
        }

        usuario.RegistrarLoginExitoso();
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        var token = _jwt.GenerarToken(usuario);
        return new LoginResponse(token.Token, token.ExpiraEn, usuario.NombreUsuario, usuario.Rol.ToString());
    }
}
