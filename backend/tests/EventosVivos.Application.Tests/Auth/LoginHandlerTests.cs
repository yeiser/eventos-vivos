using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Auth;
using EventosVivos.Application.Auth.Login;
using EventosVivos.Application.Common;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Usuarios;

namespace EventosVivos.Application.Tests.Auth;

public class LoginHandlerTests
{
    private readonly IUsuarioRepository _usuarios = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RelojFijo _clock = Datos.Reloj();
    private readonly LoginValidator _validator = new();

    private static Usuario CrearUsuario() =>
        new(Guid.NewGuid(), "admin", Email.Crear("admin@correo.com"), "hash", Rol.Admin);

    private LoginHandler Handler() => new(_usuarios, _hasher, _jwt, _uow, _clock, _validator);

    private static LoginCommand Cmd(string password = "Secreta123!") => new("admin", password);

    [Fact]
    public async Task Login_correcto_devuelve_token()
    {
        var usuario = CrearUsuario();
        _usuarios.ObtenerPorNombreUsuarioAsync("admin", Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verificar(Arg.Any<string>(), "hash").Returns(true);
        _jwt.GenerarToken(usuario).Returns(new TokenAcceso("token-jwt", _clock.Now.AddHours(2)));

        var dto = await Handler().EjecutarAsync(Cmd());

        dto.Token.Should().Be("token-jwt");
        dto.Rol.Should().Be("Admin");
        await _uow.Received().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Usuario_inexistente_lanza_CredencialesInvalidas()
    {
        _usuarios.ObtenerPorNombreUsuarioAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Usuario?)null);

        var act = () => Handler().EjecutarAsync(Cmd());

        await act.Should().ThrowAsync<CredencialesInvalidasException>();
    }

    [Fact]
    public async Task Password_incorrecta_incrementa_intentos_y_lanza_CredencialesInvalidas()
    {
        var usuario = CrearUsuario();
        _usuarios.ObtenerPorNombreUsuarioAsync("admin", Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verificar(Arg.Any<string>(), "hash").Returns(false);

        var act = () => Handler().EjecutarAsync(Cmd("malo"));

        await act.Should().ThrowAsync<CredencialesInvalidasException>();
        usuario.IntentosFallidos.Should().Be(1);
        await _uow.Received().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task El_intento_que_alcanza_el_umbral_lanza_CuentaBloqueada()
    {
        var usuario = CrearUsuario();
        for (var i = 0; i < Usuario.MaxIntentosFallidos - 1; i++)
            usuario.RegistrarIntentoFallido(_clock); // 4 fallos previos
        _usuarios.ObtenerPorNombreUsuarioAsync("admin", Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verificar(Arg.Any<string>(), "hash").Returns(false);

        var act = () => Handler().EjecutarAsync(Cmd("malo")); // 5º fallo

        await act.Should().ThrowAsync<CuentaBloqueadaException>();
        usuario.EstaBloqueado(_clock).Should().BeTrue();
    }

    [Fact]
    public async Task Cuenta_bloqueada_lanza_CuentaBloqueada_sin_verificar_password()
    {
        var usuario = CrearUsuario();
        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
            usuario.RegistrarIntentoFallido(_clock); // ya bloqueada
        _usuarios.ObtenerPorNombreUsuarioAsync("admin", Arg.Any<CancellationToken>()).Returns(usuario);

        var act = () => Handler().EjecutarAsync(Cmd());

        await act.Should().ThrowAsync<CuentaBloqueadaException>();
        _hasher.DidNotReceive().Verificar(Arg.Any<string>(), Arg.Any<string>());
    }
}
