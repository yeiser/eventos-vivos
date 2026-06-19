using EventosVivos.Domain.Usuarios;

namespace EventosVivos.Application.Abstractions;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(string nombreUsuario, CancellationToken cancellationToken = default);

    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
