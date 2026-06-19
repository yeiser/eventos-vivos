using EventosVivos.Domain.Auditoria;
using EventosVivos.Domain.Eventos;
using EventosVivos.Domain.Reservas;
using EventosVivos.Domain.Usuarios;
using EventosVivos.Domain.Venues;
using EventosVivos.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence;

public sealed class EventosDbContext : DbContext
{
    public EventosDbContext(DbContextOptions<EventosDbContext> options) : base(options) { }

    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EventosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todos los DateTimeOffset se normalizan a UTC al persistir (requisito de Npgsql/timestamptz).
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcNullableDateTimeOffsetConverter>();
    }
}
