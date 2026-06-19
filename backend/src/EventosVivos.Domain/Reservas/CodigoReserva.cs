using System.Text.RegularExpressions;
using EventosVivos.Domain.Common;

namespace EventosVivos.Domain.Reservas;

/// <summary>
/// Value Object del código de reserva con formato <c>EV-{6 dígitos}</c> (RF-04).
/// La unicidad se garantiza en infraestructura (índice único + reintento); aquí solo el formato.
/// </summary>
public sealed partial record CodigoReserva
{
    public const string Prefijo = "EV-";
    public string Valor { get; }

    private CodigoReserva(string valor) => Valor = valor;

    public static CodigoReserva Crear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !PatronCodigo().IsMatch(valor.Trim()))
            throw new DatosInvalidosException($"El código de reserva '{valor}' no cumple el formato EV-NNNNNN.");

        return new CodigoReserva(valor.Trim().ToUpperInvariant());
    }

    /// <summary>Construye el código a partir de un número de 0 a 999999 (lo usa el generador de infraestructura).</summary>
    public static CodigoReserva DesdeNumero(int numero)
    {
        if (numero is < 0 or > 999999)
            throw new DatosInvalidosException("El número del código de reserva debe estar entre 0 y 999999.");

        return new CodigoReserva($"{Prefijo}{numero:D6}");
    }

    public override string ToString() => Valor;

    [GeneratedRegex(@"^EV-\d{6}$", RegexOptions.IgnoreCase)]
    private static partial Regex PatronCodigo();
}
