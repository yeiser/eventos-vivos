using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Commands.CrearReserva;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Reservas;
using FluentValidation;

namespace EventosVivos.Application.Tests.Reservas;

public class CrearReservaHandlerTests
{
    private readonly IEventoRepository _eventos = Substitute.For<IEventoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly RelojFijo _clock = Datos.Reloj();
    private readonly CrearReservaValidator _validator = new();

    public CrearReservaHandlerTests()
    {
        // La transacción de la UoW simplemente ejecuta la operación en las pruebas.
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<ReservaDto>>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task<ReservaDto>>>().Invoke(CancellationToken.None));
    }

    private CrearReservaHandler Handler() => new(_eventos, _uow, _clock, _validator);

    [Fact]
    public async Task Reserva_valida_devuelve_dto_pendiente()
    {
        var evento = Datos.Evento(_clock, capacidad: 100);
        _eventos.ObtenerParaReservaAsync(evento.Id, Arg.Any<CancellationToken>()).Returns(evento);

        var dto = await Handler().EjecutarAsync(Datos.CrearReservaCmd(evento.Id, cantidad: 3));

        dto.Estado.Should().Be(EstadoReserva.PendientePago);
        dto.Cantidad.Should().Be(3);
        await _uow.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lanza_NotFound_si_el_evento_no_existe()
    {
        _eventos.ObtenerParaReservaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Evento?)null);

        var act = () => Handler().EjecutarAsync(Datos.CrearReservaCmd(Guid.NewGuid()));

        await act.Should().ThrowAsync<RecursoNoEncontradoException>();
    }

    [Fact]
    public async Task Propaga_ValidationException_con_cantidad_invalida()
    {
        var act = () => Handler().EjecutarAsync(Datos.CrearReservaCmd(Guid.NewGuid(), cantidad: 0));

        await act.Should().ThrowAsync<ValidationException>();
    }
}
