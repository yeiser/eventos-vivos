using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;

namespace EventosVivos.Domain.Tests.Eventos;

public class PeriodoEventoTests
{
    private static readonly DateTimeOffset T = Dominio.Ahora;

    [Fact]
    public void Crear_con_fin_posterior_al_inicio_es_valido()
    {
        var periodo = PeriodoEvento.Crear(T, T.AddHours(2));

        periodo.Duracion.Should().Be(TimeSpan.FromHours(2));
    }

    [Theory]
    [InlineData(0)]    // fin == inicio
    [InlineData(-1)]   // fin < inicio
    public void Crear_con_fin_no_posterior_lanza_DatosInvalidos(int horas)
    {
        var act = () => PeriodoEvento.Crear(T, T.AddHours(horas));

        act.Should().Throw<DatosInvalidosException>();
    }

    [Fact]
    public void SeSuperponeCon_intervalos_solapados_devuelve_true()
    {
        var a = PeriodoEvento.Crear(T, T.AddHours(3));
        var b = PeriodoEvento.Crear(T.AddHours(2), T.AddHours(5));

        a.SeSuperponeCon(b).Should().BeTrue();
        b.SeSuperponeCon(a).Should().BeTrue();
    }

    [Fact]
    public void SeSuperponeCon_contacto_en_extremos_no_es_superposicion()
    {
        // a termina justo cuando b empieza.
        var a = PeriodoEvento.Crear(T, T.AddHours(2));
        var b = PeriodoEvento.Crear(T.AddHours(2), T.AddHours(4));

        a.SeSuperponeCon(b).Should().BeFalse();
        b.SeSuperponeCon(a).Should().BeFalse();
    }

    [Fact]
    public void SeSuperponeCon_intervalos_disjuntos_devuelve_false()
    {
        var a = PeriodoEvento.Crear(T, T.AddHours(1));
        var b = PeriodoEvento.Crear(T.AddHours(3), T.AddHours(4));

        a.SeSuperponeCon(b).Should().BeFalse();
    }
}
