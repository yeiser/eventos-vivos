using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Venues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventosVivos.Infrastructure.Persistence.Configurations;

public sealed class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("eventos");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Titulo).HasMaxLength(Evento.TituloMaximo).IsRequired();
        builder.Property(e => e.Descripcion).HasMaxLength(Evento.DescripcionMaxima).IsRequired();
        builder.Property(e => e.VenueId).IsRequired();
        builder.Property(e => e.CapacidadMaxima).IsRequired();
        builder.Property(e => e.FechaInicio).IsRequired();
        builder.Property(e => e.FechaFin).IsRequired();
        builder.Property(e => e.Precio).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();

        // El periodo es un VO computado a partir de las fechas: no se mapea.
        builder.Ignore(e => e.Periodo);

        builder.ConfigurarAuditoria();

        // Anti-sobreventa: token de concurrencia optimista usando la columna de sistema xmin de PostgreSQL.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Relación de agregado: Evento -> Reservas (acceso por campo _reservas).
        builder.HasMany(e => e.Reservas)
            .WithOne()
            .HasForeignKey(r => r.EventoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(e => e.Reservas)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasField("_reservas");

        // FK al venue (sin propiedad de navegación en el dominio).
        builder.HasOne<Venue>()
            .WithMany()
            .HasForeignKey(e => e.VenueId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para RN02 (solapamiento por venue) y filtros/búsqueda de RF-02.
        builder.HasIndex(e => new { e.VenueId, e.FechaInicio });
        builder.HasIndex(e => e.Titulo);
        builder.HasIndex(e => e.Estado);
    }
}
