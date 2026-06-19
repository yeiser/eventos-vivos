using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Tests.Common;

public class EmailTests
{
    [Theory]
    [InlineData("ana@correo.com")]
    [InlineData("ANA.perez@Sub.Dominio.CO")]
    [InlineData("  juan@correo.com  ")]
    public void Crear_con_email_valido_normaliza_a_minusculas_y_trim(string entrada)
    {
        var email = Email.Crear(entrada);

        email.Valor.Should().Be(entrada.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]              // host sin punto
    [InlineData("a@@b.com")]
    [InlineData("texto suelto")]
    public void Crear_con_email_invalido_lanza_DatosInvalidos(string? entrada)
    {
        var act = () => Email.Crear(entrada);

        act.Should().Throw<DatosInvalidosException>();
    }

    [Fact]
    public void Dos_emails_con_mismo_valor_son_iguales_por_valor()
    {
        Email.Crear("ana@correo.com").Should().Be(Email.Crear("ANA@correo.com"));
    }
}
