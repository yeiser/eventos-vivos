using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos;
using EventosVivos.Application.Eventos.Queries;
using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Tests.Eventos;

public class QueriesHandlerTests
{
    private readonly IEventoRepository _eventos = Substitute.For<IEventoRepository>();
    private readonly RelojFijo _clock = Datos.Reloj();

    [Fact]
    public async Task ListarEventos_mapea_a_resultado_paginado()
    {
        var lista = new List<Evento> { Datos.Evento(_clock), Datos.Evento(_clock) };
        _eventos.ListarAsync(Arg.Any<EventoFiltro>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Evento>)lista, 5));

        var handler = new ListarEventosHandler(_eventos, _clock);
        var resultado = await handler.EjecutarAsync(new EventoFiltro { Pagina = 1, TamanoPagina = 2 });

        resultado.Items.Should().HaveCount(2);
        resultado.Total.Should().Be(5);
        resultado.TotalPaginas.Should().Be(3);
    }

    [Fact]
    public async Task ReporteOcupacion_devuelve_metricas_del_evento()
    {
        var evento = Datos.Evento(_clock, capacidad: 100, precio: 50m);
        var reserva = evento.Reservar(10, "Ana", EventosVivos.Domain.Common.Email.Crear("ana@correo.com"), _clock);
        evento.ConfirmarPagoReserva(reserva.Id, new GeneradorCodigoFalso(), _clock);
        _eventos.ObtenerPorIdAsync(evento.Id, Arg.Any<CancellationToken>()).Returns(evento);

        var handler = new ReporteOcupacionHandler(_eventos, _clock);
        var reporte = await handler.EjecutarAsync(evento.Id);

        reporte.EntradasVendidas.Should().Be(10);
        reporte.EntradasDisponibles.Should().Be(90);
        reporte.IngresosTotales.Should().Be(500m);
        reporte.PorcentajeOcupacion.Should().Be(10m);
    }

    [Fact]
    public async Task ObtenerEvento_lanza_NotFound_si_no_existe()
    {
        _eventos.ObtenerPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Evento?)null);
        var handler = new ObtenerEventoHandler(_eventos, _clock);

        var act = () => handler.EjecutarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<RecursoNoEncontradoException>();
    }
}
