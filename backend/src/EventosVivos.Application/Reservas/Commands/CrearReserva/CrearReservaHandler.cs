using EventosVivos.Application.Abstractions;
using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Application.Reservas.Mapping;
using EventosVivos.Domain.Common;
using FluentValidation;

namespace EventosVivos.Application.Reservas.Commands.CrearReserva;

/// <summary>
/// RF-03: crea una reserva. Se ejecuta dentro de una transacción con bloqueo pesimista del evento
/// (ObtenerParaReservaAsync) para impedir la sobreventa bajo reservas concurrentes (§13).
/// </summary>
public sealed class CrearReservaHandler
{
    private readonly IEventoRepository _eventos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IValidator<CrearReservaCommand> _validator;

    public CrearReservaHandler(
        IEventoRepository eventos,
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<CrearReservaCommand> validator)
    {
        _eventos = eventos;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
    }

    public async Task<ReservaDto> EjecutarAsync(CrearReservaCommand command, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var email = Email.Crear(command.EmailComprador);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var evento = await _eventos.ObtenerParaReservaAsync(command.EventoId, ct)
                ?? throw new RecursoNoEncontradoException("evento", command.EventoId);

            // El dominio valida RN04, RF-03 (24h/5), RN05 (precio/10), composición A-02 y aforo.
            var reserva = evento.Reservar(command.Cantidad, command.NombreComprador, email, _clock);

            await _unitOfWork.GuardarCambiosAsync(ct);
            return reserva.ToDto();
        }, cancellationToken);
    }
}
