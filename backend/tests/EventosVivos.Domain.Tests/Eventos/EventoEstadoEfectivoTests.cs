using EventosVivos.Domain.Eventos;

namespace EventosVivos.Domain.Tests.Eventos;

public class EventoEstadoEfectivoTests
{
    [Fact]
    public void Evento_activo_antes_del_fin_sigue_activo()
    {
        var reloj = Dominio.Reloj();
        var evento = Dominio.Evento(reloj); // inicia en 10 días

        evento.EstadoEfectivo(reloj).Should().Be(EstadoEvento.Activo);
    }

    [Fact]
    public void Evento_activo_despues_del_fin_se_considera_completado()
    {
        var reloj = Dominio.Reloj();
        var evento = Dominio.Evento(reloj, inicio: reloj.Now.AddHours(2), duracion: TimeSpan.FromHours(1));

        reloj.Avanzar(TimeSpan.FromHours(4)); // ya pasó el fin

        evento.EstadoEfectivo(reloj).Should().Be(EstadoEvento.Completado);
    }

    [Fact]
    public void En_el_instante_exacto_del_fin_sigue_activo()
    {
        var reloj = Dominio.Reloj();
        var inicio = reloj.Now.AddHours(2);
        var evento = Dominio.Evento(reloj, inicio: inicio, duracion: TimeSpan.FromHours(1));

        reloj.Now = inicio.AddHours(1); // exactamente el fin

        evento.EstadoEfectivo(reloj).Should().Be(EstadoEvento.Activo);
    }

    [Fact]
    public void Evento_cancelado_nunca_pasa_a_completado()
    {
        var reloj = Dominio.Reloj();
        var evento = Dominio.Evento(reloj, inicio: reloj.Now.AddHours(2), duracion: TimeSpan.FromHours(1));
        evento.Cancelar(reloj);

        reloj.Avanzar(TimeSpan.FromDays(30)); // mucho después del fin

        evento.EstadoEfectivo(reloj).Should().Be(EstadoEvento.Cancelado);
    }

    [Fact]
    public void Cancelar_un_evento_ya_completado_lanza_EstadoInvalido()
    {
        var reloj = Dominio.Reloj();
        var evento = Dominio.Evento(reloj, inicio: reloj.Now.AddHours(2), duracion: TimeSpan.FromHours(1));
        reloj.Avanzar(TimeSpan.FromHours(5));

        var act = () => evento.Cancelar(reloj);

        act.Should().Throw<EventosVivos.Domain.Common.EstadoInvalidoException>();
    }
}
