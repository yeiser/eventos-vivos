using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EventosVivos.Infrastructure.Persistence.Converters;

/// <summary>
/// Normaliza los DateTimeOffset a UTC al escribir (Npgsql exige offset 0 para 'timestamptz').
/// Las reglas sensibles a la hora local (p. ej. RN03) ya se validaron en el dominio antes de persistir.
/// </summary>
public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(v => v.ToUniversalTime(), v => v) { }
}

public sealed class UtcNullableDateTimeOffsetConverter : ValueConverter<DateTimeOffset?, DateTimeOffset?>
{
    public UtcNullableDateTimeOffsetConverter()
        : base(v => v.HasValue ? v.Value.ToUniversalTime() : v, v => v) { }
}
