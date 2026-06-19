using EventosVivos.Application.Abstractions;

namespace EventosVivos.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly EventosDbContext _db;

    public UnitOfWork(EventosDbContext db) => _db = db;

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var resultado = await operacion(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            await transaccion.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
