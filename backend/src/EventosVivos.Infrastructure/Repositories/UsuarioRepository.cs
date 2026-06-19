using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Usuarios;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly EventosDbContext _db;

    public UsuarioRepository(EventosDbContext db) => _db = db;

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancellationToken);

    public Task<bool> ExisteAsync(string nombreUsuario, CancellationToken cancellationToken = default) =>
        _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, cancellationToken);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        await _db.Usuarios.AddAsync(usuario, cancellationToken);
}
