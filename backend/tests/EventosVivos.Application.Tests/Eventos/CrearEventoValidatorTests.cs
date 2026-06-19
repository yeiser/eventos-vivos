using EventosVivos.Application.Eventos.Commands.CrearEvento;
using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Tests.Eventos;

public class CrearEventoValidatorTests
{
    private readonly CrearEventoValidator _validator = new();

    [Fact]
    public void Comando_valido_pasa_la_validacion()
    {
        var result = _validator.Validate(Datos.CrearEventoCmd());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("abcd")]                       // 4 chars
    [InlineData("")]                           // vacío
    public void Titulo_invalido_falla(string titulo)
    {
        var result = _validator.Validate(Datos.CrearEventoCmd(titulo: titulo));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CrearEventoCommand.Titulo));
    }

    [Fact]
    public void Capacidad_no_positiva_falla()
    {
        _validator.Validate(Datos.CrearEventoCmd(capacidad: 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Precio_no_positivo_falla()
    {
        _validator.Validate(Datos.CrearEventoCmd(precio: 0)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fin_no_posterior_al_inicio_falla()
    {
        var ini = Datos.Ahora.AddDays(5);
        var cmd = new CrearEventoCommand("Titulo válido", "Descripción válida y larga.", 1, 50, ini, ini, 30m, TipoEvento.Taller);

        _validator.Validate(cmd).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Tipo_invalido_falla()
    {
        _validator.Validate(Datos.CrearEventoCmd(tipo: (TipoEvento)999)).IsValid.Should().BeFalse();
    }
}
