using EventosVivos.Domain.Common;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

/// <summary>Mapeo común de los campos de trazabilidad (IAuditableEntity / EntidadAuditable).</summary>
internal static class AuditoriaConfig
{
    public static void ConfigurarAuditoria<T>(this EntityTypeBuilder<T> builder) where T : EntidadAuditable
    {
        builder.Property(e => e.FechaCreacion);
        builder.Property(e => e.CreadoPor).HasMaxLength(200);
        builder.Property(e => e.FechaUltimaModificacion);
        builder.Property(e => e.ModificadoPor).HasMaxLength(200);
    }
}
