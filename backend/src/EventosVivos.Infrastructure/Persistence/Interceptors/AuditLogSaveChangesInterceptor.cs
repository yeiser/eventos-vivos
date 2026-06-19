using System.Diagnostics;
using System.Text.Json;
using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Auditoria;
using EventosVivos.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EventosVivos.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Genera el audit trail inmutable (§16.2): por cada entidad auditable creada/modificada/eliminada
/// inserta un <see cref="AuditLog"/> con valores antes/después, campos modificados, traceId e IP.
/// Los campos sensibles (p. ej. el hash de contraseña) se enmascaran.
/// </summary>
public sealed class AuditLogSaveChangesInterceptor : SaveChangesInterceptor
{
    private const string Mascara = "***";
    private static readonly HashSet<string> CamposSensibles = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash"
    };

    private readonly ICurrentUserService _usuario;
    private readonly IClock _clock;

    public AuditLogSaveChangesInterceptor(ICurrentUserService usuario, IClock clock)
    {
        _usuario = usuario;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Auditar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Auditar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Auditar(DbContext? context)
    {
        if (context is null)
            return;

        var actor = _usuario.NombreUsuario;
        var ip = _usuario.IpOrigen;
        var traceId = Activity.Current?.Id;
        var ahora = _clock.Now;

        var entradas = context.ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (entradas.Count == 0)
            return;

        var logs = new List<AuditLog>(entradas.Count);

        foreach (var entry in entradas)
        {
            var accion = entry.State switch
            {
                EntityState.Added => AccionAuditoria.Crear,
                EntityState.Deleted => AccionAuditoria.Eliminar,
                _ => AccionAuditoria.Actualizar
            };

            var (antes, despues, campos) = Serializar(entry);

            logs.Add(new AuditLog(
                Guid.NewGuid(),
                entry.Entity.GetType().Name,
                ObtenerId(entry),
                accion,
                actor,
                ahora,
                antes,
                despues,
                campos,
                traceId,
                ip));
        }

        context.Set<AuditLog>().AddRange(logs);
    }

    private static string ObtenerId(EntityEntry entry) =>
        entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? string.Empty;

    private static (string? Antes, string? Despues, string? Campos) Serializar(EntityEntry entry)
    {
        var antes = new Dictionary<string, string?>();
        var despues = new Dictionary<string, string?>();
        var campos = new List<string>();

        foreach (var prop in entry.Properties)
        {
            var nombre = prop.Metadata.Name;
            var sensible = CamposSensibles.Contains(nombre);

            switch (entry.State)
            {
                case EntityState.Added:
                    despues[nombre] = Valor(prop.CurrentValue, sensible);
                    break;
                case EntityState.Deleted:
                    antes[nombre] = Valor(prop.OriginalValue, sensible);
                    break;
                default: // Modified
                    if (prop.IsModified)
                        campos.Add(nombre);
                    antes[nombre] = Valor(prop.OriginalValue, sensible);
                    despues[nombre] = Valor(prop.CurrentValue, sensible);
                    break;
            }
        }

        return (
            antes.Count > 0 ? JsonSerializer.Serialize(antes) : null,
            despues.Count > 0 ? JsonSerializer.Serialize(despues) : null,
            campos.Count > 0 ? JsonSerializer.Serialize(campos) : null);
    }

    private static string? Valor(object? valor, bool sensible) =>
        sensible ? Mascara : valor?.ToString();
}
