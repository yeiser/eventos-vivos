using EventosVivos.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever(); // datos de referencia con id fijo
        builder.Property(v => v.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Capacidad).IsRequired();
        builder.Property(v => v.Ciudad).HasMaxLength(100).IsRequired();
    }
}
