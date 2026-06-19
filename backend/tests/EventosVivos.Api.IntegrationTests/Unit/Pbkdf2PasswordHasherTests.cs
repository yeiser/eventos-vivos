using EventosVivos.Infrastructure.Auth;
using FluentAssertions;
using Xunit;

namespace EventosVivos.Api.IntegrationTests.Unit;

// Test unitario puro (no necesita base de datos ni Testcontainers).
public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_produce_el_formato_iteraciones_salt_hash()
    {
        var hash = _hasher.Hash("Secreta123!");

        var partes = hash.Split('.');
        partes.Should().HaveCount(3);
        partes[0].Should().Be("100000");
    }

    [Fact]
    public void Hash_de_la_misma_clave_genera_valores_distintos_por_el_salt()
    {
        _hasher.Hash("Secreta123!").Should().NotBe(_hasher.Hash("Secreta123!"));
    }

    [Fact]
    public void Verificar_acepta_la_contraseña_correcta()
    {
        var hash = _hasher.Hash("Secreta123!");

        _hasher.Verificar("Secreta123!", hash).Should().BeTrue();
    }

    [Fact]
    public void Verificar_rechaza_una_contraseña_incorrecta()
    {
        var hash = _hasher.Hash("Secreta123!");

        _hasher.Verificar("Otra456?", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData("", "100000.AAAA.BBBB")]
    [InlineData("x", "")]
    [InlineData("x", "formato-invalido")]
    [InlineData("x", "noEsNumero.AAAA.BBBB")]
    [InlineData("x", "100000.no-base64!.BBBB")]
    public void Verificar_devuelve_false_ante_entradas_invalidas(string password, string hash)
    {
        _hasher.Verificar(password, hash).Should().BeFalse();
    }
}
