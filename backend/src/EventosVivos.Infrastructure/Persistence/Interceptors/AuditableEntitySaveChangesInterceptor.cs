using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EventosVivos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Rellena automáticamente los campos de trazabilidad (creado/modificado por quién y cuándo) de las
/// entidades <see cref="IAuditableEntity"/> antes de guardar (§16.1). La lógica de dominio no los gestiona.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _usuario;
    private readonly IClock _clock;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService usuario, IClock clock)
    {
        _usuario = usuario;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Aplicar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Aplicar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Aplicar(DbContext? context)
    {
        if (context is null)
            return;

        var ahora = _clock.Now;
        var actor = _usuario.NombreUsuario;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.EstablecerCreacion(ahora, actor);
            else if (entry.State == EntityState.Modified)
                entry.Entity.EstablecerModificacion(ahora, actor);
        }
    }
}
