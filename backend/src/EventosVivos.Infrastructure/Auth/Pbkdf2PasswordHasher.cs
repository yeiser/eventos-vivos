using System.Security.Cryptography;
using EventosVivos.Application.Abstractions;

namespace EventosVivos.Infrastructure.Auth;

/// <summary>
/// Hash de contraseñas con PBKDF2 (SHA-256, salt aleatorio por contraseña). Formato persistido:
/// <c>{iteraciones}.{saltBase64}.{hashBase64}</c>. Comparación en tiempo constante.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iteraciones = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var clave = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iteraciones, Algoritmo, KeySize);

        return $"{Iteraciones}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(clave)}";
    }

    public bool Verificar(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        var partes = hash.Split('.', 3);
        if (partes.Length != 3 || !int.TryParse(partes[0], out var iteraciones))
            return false;

        byte[] salt, clave;
        try
        {
            salt = Convert.FromBase64String(partes[1]);
            clave = Convert.FromBase64String(partes[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var claveCalculada = Rfc2898DeriveBytes.Pbkdf2(password, salt, iteraciones, Algoritmo, clave.Length);
        return CryptographicOperations.FixedTimeEquals(claveCalculada, clave);
    }
}
