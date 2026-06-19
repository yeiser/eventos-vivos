using EventosVivos.Infrastructure.Auth;
using EventosVivos.Infrastructure.Persistence;
using EventosVivos.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EventosVivos.Api.IntegrationTests.Soporte;

/// <summary>
/// Levanta un PostgreSQL efímero (Testcontainers), aplica las migraciones y siembra los datos de
/// referencia una sola vez para toda la colección de pruebas de repositorio.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public DbContextOptions<EventosDbContext> Options { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Options = new DbContextOptionsBuilder<EventosDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = CrearContexto();
        await db.Database.MigrateAsync();

        var seeder = new DatabaseSeeder(db, new Pbkdf2PasswordHasher());
        await seeder.SeedAsync();
    }

    public EventosDbContext CrearContexto() => new(Options);

    /// <summary>Limpia eventos y reservas entre pruebas (los venues/usuarios sembrados se conservan).</summary>
    public async Task LimpiarEventosAsync()
    {
        await using var db = CrearContexto();
        await db.Reservas.ExecuteDeleteAsync();
        await db.Eventos.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
