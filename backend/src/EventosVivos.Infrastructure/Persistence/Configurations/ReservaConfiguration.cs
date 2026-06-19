using EventosVivos.Domain.Common;
using EventosVivos.Domain.Reservas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("reservas");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.EventoId).IsRequired();
        builder.Property(r => r.Cantidad).IsRequired();
        builder.Property(r => r.NombreComprador).HasMaxLength(200).IsRequired();

        builder.Property(r => r.EmailComprador)
            .HasConversion(e => e.Valor, v => Email.Crear(v))
            .HasColumnName("email_comprador")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(r => r.Codigo)
            .HasConversion(c => c!.Valor, v => CodigoReserva.Crear(v))
            .HasColumnName("codigo")
            .HasMaxLength(9);

        builder.Property(r => r.FechaReserva).IsRequired();
        builder.Property(r => r.FechaConfirmacion);
        builder.Property(r => r.FechaCancelacion);

        builder.Ignore(r => r.OcupaCapacidad);

        builder.ConfigurarAuditoria();

        // RF-04: el código de reserva es único (nulos múltiples permitidos en PostgreSQL).
        builder.HasIndex(r => r.Codigo).IsUnique();
        builder.HasIndex(r => r.EventoId);
        builder.HasIndex(r => r.Estado);
    }
}
