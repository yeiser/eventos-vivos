using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Eventos.Dtos;
using EventosVivos.Application.Eventos.Mapping;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Eventos;
using FluentValidation;

namespace EventosVivos.Application.Eventos.Commands.CrearEvento;

public sealed class CrearEventoHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IVenueRepository _venues;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CrearEventoCommand> _validator;

    public CrearEventoHandler(
        IEventoRepository eventos,
        IVenueRepository venues,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CrearEventoCommand> validator)
    {
        _eventos = eventos;
        _venues = venues;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<EventoDto> EjecutarAsync(CrearEventoCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var venue = await _venues.ObtenerPorIdAsync(command.VenueId, cancellationToken)
            ?? throw new RecursoNoEncontradoException("venue", command.VenueId);

        // El dominio valida RN01 (capacidad ≤ venue), RN03 (horario), fecha futura y fin > inicio.
        var evento = Evento.Crear(
            command.Titulo, command.Descripcion, venue, command.CapacidadMaxima,
            command.FechaInicio, command.FechaFin, command.Precio, command.Tipo, _clock);

        // RN02: no puede haber otro evento activo en el mismo venue con horario superpuesto.
        var haySolapamiento = await _eventos.ExisteSolapamientoAsync(
            command.VenueId, evento.FechaInicio, evento.FechaFin, excluirEventoId: null, cancellationToken);
        if (haySolapamiento)
            throw new ReglaNegocioException("RN02",
                "Ya existe un evento activo en ese venue con un horario superpuesto.");

        await _eventos.AgregarAsync(evento, cancellationToken);
        await _unitOfWork.GuardarCambiosAsync(cancellationToken);

        return evento.ToDto(_clock);
    }
}
