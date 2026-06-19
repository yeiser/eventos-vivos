using EventosVivos.Domain.Common;
using EventosVivos.Domain.Usuarios;

namespace EventosVivos.Domain.Tests.Usuarios;

public class UsuarioLockoutTests
{
    private static readonly DateTimeOffset Ahora = Dominio.Ahora;

    private static Usuario Crear() =>
        new(Guid.NewGuid(), "usuario", Email.Crear("u@correo.com"), "hash", Rol.Usuario);

    [Fact]
    public void Una_cuenta_nueva_no_esta_bloqueada()
    {
        var reloj = new RelojFalso(Ahora);

        Crear().EstaBloqueado(reloj).Should().BeFalse();
    }

    [Fact]
    public void Cuatro_intentos_fallidos_no_bloquean()
    {
        var reloj = new RelojFalso(Ahora);
        var usuario = Crear();

        for (var i = 0; i < 4; i++)
            usuario.RegistrarIntentoFallido(reloj);

        usuario.EstaBloqueado(reloj).Should().BeFalse();
        usuario.IntentosFallidos.Should().Be(4);
    }

    [Fact]
    public void Al_quinto_intento_fallido_la_cuenta_se_bloquea()
    {
        var reloj = new RelojFalso(Ahora);
        var usuario = Crear();

        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
            usuario.RegistrarIntentoFallido(reloj);

        usuario.EstaBloqueado(reloj).Should().BeTrue();
        usuario.BloqueadoHasta.Should().Be(Ahora.Add(Usuario.DuracionBloqueo));
    }

    [Fact]
    public void El_bloqueo_expira_pasada_la_duracion()
    {
        var reloj = new RelojFalso(Ahora);
        var usuario = Crear();
        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
            usuario.RegistrarIntentoFallido(reloj);

        reloj.Avanzar(Usuario.DuracionBloqueo + TimeSpan.FromMinutes(1));

        usuario.EstaBloqueado(reloj).Should().BeFalse();
    }

    [Fact]
    public void Tras_expirar_el_bloqueo_el_contador_se_reinicia_en_el_siguiente_fallo()
    {
        var reloj = new RelojFalso(Ahora);
        var usuario = Crear();
        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
            usuario.RegistrarIntentoFallido(reloj);

        reloj.Avanzar(Usuario.DuracionBloqueo + TimeSpan.FromMinutes(1));
        usuario.RegistrarIntentoFallido(reloj); // primer fallo tras expirar

        usuario.IntentosFallidos.Should().Be(1);
        usuario.EstaBloqueado(reloj).Should().BeFalse();
    }

    [Fact]
    public void Un_login_exitoso_reinicia_intentos_y_bloqueo()
    {
        var reloj = new RelojFalso(Ahora);
        var usuario = Crear();
        for (var i = 0; i < Usuario.MaxIntentosFallidos; i++)
            usuario.RegistrarIntentoFallido(reloj);

        usuario.RegistrarLoginExitoso();

        usuario.IntentosFallidos.Should().Be(0);
        usuario.BloqueadoHasta.Should().BeNull();
        usuario.EstaBloqueado(reloj).Should().BeFalse();
    }
}
