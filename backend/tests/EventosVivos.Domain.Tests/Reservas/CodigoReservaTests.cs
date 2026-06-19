using EventosVivos.Domain.Common;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Domain.Tests.Reservas;

public class CodigoReservaTests
{
    [Theory]
    [InlineData(123456, "EV-123456")]
    [InlineData(7, "EV-000007")]
    [InlineData(0, "EV-000000")]
    [InlineData(999999, "EV-999999")]
    public void DesdeNumero_formatea_con_prefijo_y_seis_digitos(int numero, string esperado)
    {
        CodigoReserva.DesdeNumero(numero).Valor.Should().Be(esperado);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1000000)]
    public void DesdeNumero_fuera_de_rango_lanza_DatosInvalidos(int numero)
    {
        var act = () => CodigoReserva.DesdeNumero(numero);

        act.Should().Throw<DatosInvalidosException>();
    }

    [Theory]
    [InlineData("EV-000123")]
    [InlineData("ev-000123")]
    public void Crear_con_formato_valido_normaliza_a_mayusculas(string entrada)
    {
        CodigoReserva.Crear(entrada).Valor.Should().Be("EV-000123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("EV-123")]       // pocos dígitos
    [InlineData("EV-1234567")]   // demasiados dígitos
    [InlineData("XX-123456")]    // prefijo inválido
    [InlineData("123456")]
    public void Crear_con_formato_invalido_lanza_DatosInvalidos(string entrada)
    {
        var act = () => CodigoReserva.Crear(entrada);

        act.Should().Throw<DatosInvalidosException>();
    }
}
