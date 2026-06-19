using EventosVivos.Application.Eventos;
using EventosVivos.Domain.Eventos;

namespace EventosVivos.Application.Abstractions;

public interface IEventoRepository
{
    /// <summary>Obtiene un evento con sus reservas cargadas (para reserva, confirmación y reporte).</summary>
    Task<Evento?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el evento con sus reservas adquiriendo un bloqueo pesimista de su fila (FOR UPDATE),
    /// para serializar reservas concurrentes y evitar la sobreventa. Debe llamarse dentro de una transacción.
    /// </summary>
    Task<Evento?> ObtenerParaReservaAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene el evento (raíz del agregado) que contiene la reserva indicada, con sus reservas.</summary>
    Task<Evento?> ObtenerPorReservaAsync(Guid reservaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// RN02: indica si ya existe un evento ACTIVO en el mismo venue cuyo horario se superpone con el
    /// intervalo dado (excluyendo opcionalmente un evento por su Id).
    /// </summary>
    Task<bool> ExisteSolapamientoAsync(
        int venueId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        Guid? excluirEventoId = null,
        CancellationToken cancellationToken = default);

    /// <summary>RF-02: listado paginado con filtros (búsqueda de título case-insensitive con ILIKE).</summary>
    Task<(IReadOnlyList<Evento> Items, int Total)> ListarAsync(
        EventoFiltro filtro,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(Evento evento, CancellationToken cancellationToken = default);
}
