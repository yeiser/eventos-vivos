namespace EventosVivos.Application.Abstractions;

/// <summary>Confirma los cambios pendientes como una unidad transaccional.</summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta <paramref name="operacion"/> dentro de una transacción de base de datos
    /// (commit si finaliza bien, rollback si lanza). Usado por el flujo de reserva para
    /// garantizar consistencia junto con el bloqueo de evento (anti-sobreventa, §13).
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default);
}
