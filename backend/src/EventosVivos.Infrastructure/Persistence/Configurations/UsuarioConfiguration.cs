using EventosVivos.Domain.Common;
using EventosVivos.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.NombreUsuario).HasMaxLength(100).IsRequired();

        builder.Property(u => u.Email)
            .HasConversion(e => e.Valor, v => Email.Crear(v))
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Protección anti-fuerza-bruta (lockout).
        builder.Property(u => u.IntentosFallidos).HasDefaultValue(0);
        builder.Property(u => u.BloqueadoHasta);

        builder.ConfigurarAuditoria();

        builder.HasIndex(u => u.NombreUsuario).IsUnique();
    }
}
