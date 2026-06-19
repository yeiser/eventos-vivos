using EventosVivos.Api.IntegrationTests.Soporte;
using EventosVivos.Application.Reservas.Commands.CrearReserva;
using EventosVivos.Domain.Common;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Repositories;

namespace EventosVivos.Api.IntegrationTests.Concurrencia;

/// <summary>
/// Verifica que el flujo de reserva NO permite sobreventa bajo concurrencia: con un aforo pequeño y
/// muchas reservas simultáneas, solo se confirman exactamente las que caben (§13, §17.3).
/// </summary>
[Trait("Category", "Concurrencia")]
[Collection("postgres")]
public class AntiSobreventaTests
{
    private readonly PostgresFixture _fixture;

    public AntiSobreventaTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Reservas_concurrentes_no_exceden_la_capacidad()
    {
        await _fixture.LimpiarEventosAsync();

        const int capacidad = 10;
        const int intentos = 30;

        var evento = Datos.CrearEvento(venueId: 3, capacidad: capacidad, precio: 50m, inicio: Datos.Base);
        await using (var db = _fixture.CrearContexto())
        {
            db.Eventos.Add(evento);
            await db.SaveChangesAsync();
        }

        var eventoId = evento.Id;

        // Cada reserva usa su propio contexto/conexión: compiten de verdad en la base de datos.
        async Task<bool> IntentarReservar(int indice)
        {
            await using var db = _fixture.CrearContexto();
            var handler = new CrearReservaHandler(
                new EventoRepository(db), new UnitOfWork(db), Datos.Reloj, new CrearReservaValidator());
            try
            {
                await handler.EjecutarAsync(new CrearReservaCommand(
                    eventoId, 1, $"Comprador {indice}", $"comprador{indice}@correo.com"));
                return true;
            }
            catch (ReglaNegocioException)
            {
                return false; // aforo agotado (regla CAPACIDAD)
            }
        }

        var resultados = await Task.WhenAll(Enumerable.Range(0, intentos).Select(IntentarReservar));

        var exitosas = resultados.Count(ok => ok);

        // Exactamente 'capacidad' reservas tuvieron éxito; el resto fue rechazado por aforo.
        exitosas.Should().Be(capacidad);

        await using var verificacion = _fixture.CrearContexto();
        var recargado = await new EventoRepository(verificacion).ObtenerPorIdAsync(eventoId);
        recargado!.EntradasOcupadas.Should().Be(capacidad);
        recargado.EntradasDisponibles.Should().Be(0); // nunca negativo: sin sobreventa
    }
}
