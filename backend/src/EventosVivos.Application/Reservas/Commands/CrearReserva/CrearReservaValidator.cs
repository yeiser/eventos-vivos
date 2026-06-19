using EventosVivos.Domain.Common;
using FluentValidation;

namespace EventosVivos.Application.Reservas.Commands.CrearReserva;

/// <summary>
/// Validación de entrada de RF-03 (cantidad ≥ 1, email con formato válido, nombre obligatorio).
/// Las reglas de aforo y transacción (RN04, RF-03 24h, RN05, disponibilidad) se validan en el dominio.
/// </summary>
public sealed class CrearReservaValidator : AbstractValidator<CrearReservaCommand>
{
    public CrearReservaValidator()
    {
        RuleFor(x => x.EventoId).NotEmpty();

        RuleFor(x => x.Cantidad)
            .GreaterThanOrEqualTo(1).WithMessage("La cantidad debe ser 1 o más.");

        RuleFor(x => x.NombreComprador)
            .NotEmpty().WithMessage("El nombre del comprador es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.EmailComprador)
            .NotEmpty().WithMessage("El email del comprador es obligatorio.")
            .Must(Email.EsValido).WithMessage("El email del comprador no tiene un formato válido.");
    }
}
