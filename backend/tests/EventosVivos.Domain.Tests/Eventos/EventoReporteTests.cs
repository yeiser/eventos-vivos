using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Reservas;

namespace EventosVivos.Domain.Tests.Eventos;

public class EventoReporteTests
{
    private readonly RelojFalso _reloj = Dominio.Reloj();

    [Fact]
    public void Reporte_sin_reservas_muestra_todo_disponible()
    {
        var evento = Dominio.Evento(_reloj, capacidad: 100, precio: 50m);

        var r = evento.CalcularOcupacion(_reloj);

        r.EntradasVendidas.Should().Be(0);
        r.EntradasDisponibles.Should().Be(100);
        r.PorcentajeOcupacion.Should().Be(0m);
        r.IngresosTotales.Should().Be(0m);
        r.Estado.Should().Be(EstadoEvento.Activo);
    }

    [Fact]
    public void Reporte_cuenta_solo_confirmadas_para_ventas_e_ingresos_y_ocupadas_para_disponibilidad()
    {
        // Evento a 30h (entre 24 y 48h): sin límite de 5 (>24h) y cancelar confirmada -> perdida (<48h).
        var evento = Dominio.Evento(_reloj, capacidad: 100, precio: 50m,
            inicio: _reloj.Now.AddHours(30), duracion: TimeSpan.FromHours(2));

        var pendiente = evento.Reservar(10, "Ana", Dominio.Email("ana@correo.com"), _reloj);     // pendiente: ocupa
        var confirmada = evento.Reservar(20, "Beto", Dominio.Email("beto@correo.com"), _reloj);  // confirmada: vende
        var perdida = evento.Reservar(5, "Caro", Dominio.Email("caro@correo.com"), _reloj);      // confirmada->perdida: ocupa
        var cancelada = evento.Reservar(8, "Dani", Dominio.Email("dani@correo.com"), _reloj);    // pendiente->cancelada: libera

        evento.ConfirmarPagoReserva(confirmada.Id, new GeneradorCodigoFalso(), _reloj);

        evento.ConfirmarPagoReserva(perdida.Id, new GeneradorCodigoFalso(900), _reloj);
        evento.CancelarReserva(perdida.Id, _reloj); // confirmada + <48h -> perdida (no libera)

        evento.CancelarReserva(cancelada.Id, _reloj); // pendiente -> cancelada (libera)

        var r = evento.CalcularOcupacion(_reloj);

        // ocupadas = 10 (pend) + 20 (conf) + 5 (perdida) = 35 ; confirmadas = 20
        r.EntradasVendidas.Should().Be(20);
        r.EntradasDisponibles.Should().Be(65);
        r.PorcentajeOcupacion.Should().Be(20m);     // 20/100*100
        r.IngresosTotales.Should().Be(1000m);       // 50 * 20
        _ = pendiente;
    }

    [Fact]
    public void Porcentaje_de_ocupacion_se_redondea_a_dos_decimales()
    {
        // 1 confirmada sobre capacidad 3 -> 33.33%
        var evento = Dominio.Evento(_reloj, capacidad: 3, precio: 10m);
        var reserva = evento.Reservar(1, "Ana", Dominio.Email(), _reloj);
        evento.ConfirmarPagoReserva(reserva.Id, new GeneradorCodigoFalso(), _reloj);

        var r = evento.CalcularOcupacion(_reloj);

        r.PorcentajeOcupacion.Should().Be(33.33m);
    }
}
