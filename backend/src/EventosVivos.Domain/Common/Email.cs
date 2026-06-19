using System.Net.Mail;

namespace EventosVivos.Domain.Common;

/// <summary>
/// Value Object que garantiza que un email tiene formato válido (RF-03).
/// Inmutable, normalizado (trim + minúsculas) y comparable por valor.
/// </summary>
public sealed record Email
{
    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Email Crear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DatosInvalidosException("El email del comprador es obligatorio.");

        var normalizado = valor.Trim();
        if (!EsValido(normalizado))
            throw new DatosInvalidosException($"El email '{valor}' no tiene un formato válido.");

        return new Email(normalizado.ToLowerInvariant());
    }

    public static bool EsValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return false;

        // MailAddress valida el formato; además exigimos un host con punto (rechaza "a@b").
        return MailAddress.TryCreate(valor.Trim(), out var direccion)
               && direccion!.Host.Contains('.');
    }

    public override string ToString() => Valor;
}
