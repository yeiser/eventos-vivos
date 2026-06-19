using EventosVivos.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Entidad).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntidadId).HasMaxLength(64).IsRequired();
        builder.Property(a => a.Accion).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.Usuario).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Fecha).IsRequired();
        builder.Property(a => a.ValoresAnteriores).HasColumnType("jsonb");
        builder.Property(a => a.ValoresNuevos).HasColumnType("jsonb");
        builder.Property(a => a.CamposModificados);
        builder.Property(a => a.TraceId).HasMaxLength(100);
        builder.Property(a => a.IpOrigen).HasMaxLength(64);

        builder.HasIndex(a => new { a.Entidad, a.EntidadId });
        builder.HasIndex(a => a.Fecha);
    }
}
