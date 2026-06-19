namespace EventosVivos.Application.Common;

/// <summary>
/// La cuenta está bloqueada temporalmente por demasiados intentos fallidos (anti-fuerza-bruta).
/// Se mapea a HTTP 423 (Locked).
/// </summary>
public sealed class CuentaBloqueadaException : Exception
{
    public DateTimeOffset BloqueadoHasta { get; }

    public CuentaBloqueadaException(DateTimeOffset bloqueadoHasta)
        : base("Cuenta bloqueada temporalmente por demasiados intentos fallidos. Inténtelo más tarde.")
    {
        BloqueadoHasta = bloqueadoHasta;
    }
}
