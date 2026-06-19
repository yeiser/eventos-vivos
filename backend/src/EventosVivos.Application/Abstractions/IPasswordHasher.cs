namespace EventosVivos.Application.Abstractions;

/// <summary>Genera y verifica hashes de contraseña (la contraseña en claro nunca se persiste).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verificar(string password, string hash);
}
