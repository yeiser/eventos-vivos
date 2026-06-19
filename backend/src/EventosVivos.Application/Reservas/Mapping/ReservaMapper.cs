using EventosVivos.Application.Common;
using EventosVivos.Application.Reservas.Dtos;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Application.Reservas.Mapping;

public static class ReservaMapper
{
    public static ReservaDto ToDto(this Reserva reserva) => new(
        reserva.Id,
        reserva.EventoId,
        reserva.Cantidad,
        reserva.NombreComprador,
        reserva.EmailComprador.Valor,
        reserva.Estado,
        reserva.Codigo?.Valor,
        reserva.FechaReserva,
        reserva.FechaConfirmacion,
        reserva.FechaCancelacion,
        AuditoriaDto.Desde(reserva));
}
