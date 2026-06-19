using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Usuarios;

/// <summary>
/// Usuario autenticable del sistema (ADR-007). El hash de la contraseña se genera en
/// infraestructura (Fase 5); el dominio solo lo almacena, nunca la contraseña en claro.
/// Incluye protección anti-fuerza-bruta mediante bloqueo de cuenta (lockout).
/// </summary>
public sealed class Usuario : EntidadAuditable
{
    /// <summary>Número de intentos fallidos consecutivos que disparan el bloqueo de la cuenta.</summary>
    public const int MaxIntentosFallidos = 5;

    /// <summary>Duración del bloqueo de la cuenta tras superar los intentos permitidos.</summary>
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string NombreUsuario { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Rol Rol { get; private set; }

    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    private Usuario() { } // EF

    public Usuario(Guid id, string nombreUsuario, Email email, string passwordHash, Rol rol)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new DatosInvalidosException("El nombre de usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DatosInvalidosException("El hash de la contraseña es obligatorio.");

        Id = id;
        NombreUsuario = nombreUsuario.Trim();
        Email = email ?? throw new DatosInvalidosException("El email del usuario es obligatorio.");
        PasswordHash = passwordHash;
        Rol = rol;
    }

    /// <summary>Indica si la cuenta está bloqueada en este instante (anti-fuerza-bruta).</summary>
    public bool EstaBloqueado(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return BloqueadoHasta is { } hasta && hasta > clock.Now;
    }

    /// <summary>
    /// Registra un intento de autenticación fallido. Si se alcanza <see cref="MaxIntentosFallidos"/>,
    /// la cuenta se bloquea durante <see cref="DuracionBloqueo"/>. Si el bloqueo anterior ya expiró,
    /// el contador se reinicia antes de contar este intento.
    /// </summary>
    public void RegistrarIntentoFallido(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (BloqueadoHasta is { } hasta && hasta <= clock.Now)
        {
            IntentosFallidos = 0;
            BloqueadoHasta = null;
        }

        IntentosFallidos++;

        if (IntentosFallidos >= MaxIntentosFallidos)
            BloqueadoHasta = clock.Now.Add(DuracionBloqueo);
    }

    /// <summary>Registra un login correcto: limpia los intentos fallidos y cualquier bloqueo.</summary>
    public void RegistrarLoginExitoso()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }
}
