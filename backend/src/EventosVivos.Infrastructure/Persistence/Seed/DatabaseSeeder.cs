using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Common;
using EventosVivos.Domain.Usuarios;
using EventosVivos.Domain.Venues;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence.Seed;

/// <summary>
/// Siembra datos de referencia idempotentemente: los 3 venues del enunciado y los usuarios de demo
/// (admin / usuario). Las contraseñas se hashean en tiempo de ejecución (no se versionan en claro).
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly EventosDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DatabaseSeeder(EventosDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedVenuesAsync(cancellationToken);
        await SeedUsuariosAsync(cancellationToken);
    }

    private async Task SeedVenuesAsync(CancellationToken cancellationToken)
    {
        if (await _db.Venues.AnyAsync(cancellationToken))
            return;

        _db.Venues.AddRange(
            new Venue(1, "Auditorio Central", 200, "Bogotá"),
            new Venue(2, "Sala Norte", 50, "Bogotá"),
            new Venue(3, "Arena Sur", 500, "Medellín"));

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedUsuariosAsync(CancellationToken cancellationToken)
    {
        await CrearSiNoExisteAsync("admin", "admin@eventosvivos.com", "Admin123!", Rol.Admin, cancellationToken);
        await CrearSiNoExisteAsync("usuario", "usuario@eventosvivos.com", "Usuario123!", Rol.Usuario, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task CrearSiNoExisteAsync(
        string nombreUsuario, string email, string password, Rol rol, CancellationToken cancellationToken)
    {
        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, cancellationToken))
            return;

        var usuario = new Usuario(Guid.NewGuid(), nombreUsuario, Email.Crear(email), _hasher.Hash(password), rol);
        await _db.Usuarios.AddAsync(usuario, cancellationToken);
    }
}
