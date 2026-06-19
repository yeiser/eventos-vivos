using EventosVivos.Domain.Eventos;
using FluentValidation;

namespace EventosVivos.Application.Eventos.Commands.CrearEvento;

/// <summary>
/// Validación de entrada de RF-01 (formato/longitudes/rangos). Las reglas que dependen del estado del
/// sistema (RN01 capacidad ≤ venue, RN02 solapamiento, RN03 horario, fecha futura) se validan en el
/// dominio/handler porque requieren el venue, otros eventos y el reloj.
/// </summary>
public sealed class CrearEventoValidator : AbstractValidator<CrearEventoCommand>
{
    public CrearEventoValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .Length(Evento.TituloMinimo, Evento.TituloMaximo);

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .Length(Evento.DescripcionMinima, Evento.DescripcionMaxima);

        RuleFor(x => x.VenueId).GreaterThan(0);

        RuleFor(x => x.CapacidadMaxima)
            .GreaterThan(0).WithMessage("La capacidad máxima debe ser un entero positivo.");

        RuleFor(x => x.Precio)
            .GreaterThan(0).WithMessage("El precio de entrada debe ser un decimal positivo.");

        RuleFor(x => x.FechaFin)
            .GreaterThan(x => x.FechaInicio)
            .WithMessage("La fecha de fin debe ser posterior a la de inicio.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de evento no es válido.");
    }
}
