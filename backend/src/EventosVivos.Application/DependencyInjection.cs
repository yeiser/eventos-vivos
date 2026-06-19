using EventosVivos.Application.Auditoria.Queries;
using EventosVivos.Application.Auth.Login;
using EventosVivos.Application.Eventos.Commands.CrearEvento;
using EventosVivos.Application.Eventos.Queries;
using EventosVivos.Application.Reservas.Commands.CancelarReserva;
using EventosVivos.Application.Reservas.Commands.ConfirmarPago;
using EventosVivos.Application.Reservas.Commands.CrearReserva;
using EventosVivos.Application.Reservas.Queries;
using EventosVivos.Application.Venues.Queries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventosVivos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Casos de uso (CQRS ligero, sin MediatR).
        services.AddScoped<CrearEventoHandler>();
        services.AddScoped<ListarEventosHandler>();
        services.AddScoped<ObtenerEventoHandler>();
        services.AddScoped<ReporteOcupacionHandler>();

        services.AddScoped<CrearReservaHandler>();
        services.AddScoped<ConfirmarPagoHandler>();
        services.AddScoped<CancelarReservaHandler>();
        services.AddScoped<ObtenerReservaHandler>();
        services.AddScoped<ListarReservasEventoHandler>();
        services.AddScoped<BuscarReservasHandler>();

        services.AddScoped<ListarVenuesHandler>();

        services.AddScoped<LoginHandler>();

        services.AddScoped<ListarAuditoriaHandler>();

        return services;
    }
}
