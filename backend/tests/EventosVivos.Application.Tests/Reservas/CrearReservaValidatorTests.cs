using EventosVivos.Application.Reservas.Commands.CrearReserva;

namespace EventosVivos.Application.Tests.Reservas;

public class CrearReservaValidatorTests
{
    private readonly CrearReservaValidator _validator = new();

    [Fact]
    public void Comando_valido_pasa()
    {
        _validator.Validate(Datos.CrearReservaCmd(Guid.NewGuid())).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Cantidad_menor_a_1_falla(int cantidad)
    {
        _validator.Validate(Datos.CrearReservaCmd(Guid.NewGuid(), cantidad: cantidad)).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("a@b")]
    [InlineData("")]
    public void Email_invalido_falla(string email)
    {
        _validator.Validate(Datos.CrearReservaCmd(Guid.NewGuid(), email: email)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Nombre_vacio_falla()
    {
        _validator.Validate(Datos.CrearReservaCmd(Guid.NewGuid(), nombre: "")).IsValid.Should().BeFalse();
    }
}
