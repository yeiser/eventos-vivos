namespace EventosVivos.Domain.Common;

/// <summary>Base de todas las excepciones de dominio (errores esperables de negocio).</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Datos de entrada/formato inválidos que rompen una invariante simple
/// (longitudes, valores obligatorios, positividad, formato de email, cantidad &lt; 1).
/// Se mapeará a HTTP 422/400.
/// </summary>
public sealed class DatosInvalidosException : DomainException
{
    public DatosInvalidosException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Transición de estado no permitida en una máquina de estados
/// (p. ej. confirmar una reserva ya confirmada — RF-04; cancelar una reserva terminal — RF-05).
/// Se mapeará a HTTP 409.
/// </summary>
public sealed class EstadoInvalidoException : DomainException
{
    public EstadoInvalidoException(string mensaje) : base(mensaje) { }
}

/// <summary>
/// Violación de una regla de negocio del catálogo (RN01..RN07 u otra regla de aforo/transacción).
/// Lleva el <see cref="Regla"/> para enriquecer el ProblemDetails. Se mapeará a HTTP 409/422.
/// </summary>
public sealed class ReglaNegocioException : DomainException
{
    public string Regla { get; }

    public ReglaNegocioException(string regla, string mensaje) : base(mensaje)
    {
        Regla = regla;
    }
}
