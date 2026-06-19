using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos.Commands.CrearEvento;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using FluentValidation;

namespace EventosVivos.Application.Tests.Eventos;

public class CrearEventoHandlerTests
{
    private readonly IEventoRepository _eventos = Substitute.For<IEventoRepository>();
    private readonly IVenueRepository _venues = Substitute.For<IVenueRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RelojFijo _clock = Datos.Reloj();
    private readonly CrearEventoValidator _validator = new();

    private CrearEventoHandler Handler() => new(_eventos, _venues, _uow, _clock, _validator);

    [Fact]
    public async Task Crea_evento_y_persiste_cuando_todo_es_valido()
    {
        _venues.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Datos.Venue(200));
        _eventos.ExisteSolapamientoAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var dto = await Handler().EjecutarAsync(Datos.CrearEventoCmd(capacidad: 100, titulo: "Jazz en vivo"));

        dto.Titulo.Should().Be("Jazz en vivo");
        dto.Estado.Should().Be(EstadoEvento.Activo);
        await _eventos.Received(1).AgregarAsync(Arg.Any<Evento>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lanza_NotFound_si_el_venue_no_existe()
    {
        _venues.ObtenerPorIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((Domain.Venues.Venue?)null);

        var act = () => Handler().EjecutarAsync(Datos.CrearEventoCmd());

        await act.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task Lanza_RN02_si_hay_solapamiento_en_el_venue()
    {
        _venues.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Datos.Venue(200));
        _eventos.ExisteSolapamientoAsync(Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => Handler().EjecutarAsync(Datos.CrearEventoCmd());

        (await act.Should().ThrowAsync<ReglaNegocioException>()).Which.Regla.Should().Be("RN02");
        await _eventos.DidNotReceive().AgregarAsync(Arg.Any<Evento>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lanza_RN01_si_la_capacidad_supera_la_del_venue()
    {
        _venues.ObtenerPorIdAsync(1, Arg.Any<CancellationToken>()).Returns(Datos.Venue(50));

        var act = () => Handler().EjecutarAsync(Datos.CrearEventoCmd(capacidad: 100));

        (await act.Should().ThrowAsync<ReglaNegocioException>()).Which.Regla.Should().Be("RN01");
    }

    [Fact]
    public async Task Lanza_ValidationException_con_entrada_invalida()
    {
        var act = () => Handler().EjecutarAsync(Datos.CrearEventoCmd(titulo: "abc"));

        await act.Should().ThrowAsync<ValidationException>();
    }
}
