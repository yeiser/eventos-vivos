namespace EventosVivos.Application.Common;

/// <summary>Credenciales de login inválidas (se mapea a HTTP 401).</summary>
public sealed class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException()
        : base("Usuario o contraseña incorrectos.") { }
}
