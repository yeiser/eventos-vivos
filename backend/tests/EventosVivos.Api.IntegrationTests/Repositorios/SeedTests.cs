using EventosVivos.Api.IntegrationTests.Soporte;
using EventosVivos.Domain.Usuarios;
using EventosVivos.Infrastructure.Repositories;

namespace EventosVivos.Api.IntegrationTests.Repositorios;

[Trait("Category", "Repositorio")]
[Collection("postgres")]
public class SeedTests
{
    private readonly PostgresFixture _fixture;

    public SeedTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task La_migracion_y_el_seed_insertan_los_tres_venues()
    {
        await using var db = _fixture.CrearContexto();
        var repo = new VenueRepository(db);

        var venues = await repo.ListarAsync();

        venues.Should().HaveCount(3);
        venues.Select(v => v.Id).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        venues.Single(v => v.Id == 1).Capacidad.Should().Be(200);
        venues.Single(v => v.Id == 2).Capacidad.Should().Be(50);
        venues.Single(v => v.Id == 3).Capacidad.Should().Be(500);
    }

    [Fact]
    public async Task El_seed_inserta_los_usuarios_admin_y_usuario_con_sus_roles()
    {
        await using var db = _fixture.CrearContexto();
        var repo = new UsuarioRepository(db);

        var admin = await repo.ObtenerPorNombreUsuarioAsync("admin");
        var usuario = await repo.ObtenerPorNombreUsuarioAsync("usuario");

        admin.Should().NotBeNull();
        admin!.Rol.Should().Be(Rol.Admin);
        admin.PasswordHash.Should().NotBeNullOrWhiteSpace();
        admin.PasswordHash.Should().NotContain("Admin123!"); // hasheada, no en claro

        usuario.Should().NotBeNull();
        usuario!.Rol.Should().Be(Rol.Usuario);
    }

    [Fact]
    public async Task El_seed_es_idempotente_no_duplica_venues()
    {
        // Ejecuta el seeder de nuevo y verifica que sigue habiendo 3 venises.
        await using var db = _fixture.CrearContexto();
        var seeder = new EventosVivos.Infrastructure.Persistence.Seed.DatabaseSeeder(
            db, new EventosVivos.Infrastructure.Auth.Pbkdf2PasswordHasher());

        await seeder.SeedAsync();

        var total = await new VenueRepository(_fixture.CrearContexto()).ListarAsync();
        total.Should().HaveCount(3);
    }
}
