using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EventosVivos.Infrastructure;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace EventosVivos.Api.IntegrationTests.Soporte;

/// <summary>
/// Arranca la API real (WebApplicationFactory) contra un PostgreSQL efímero (Testcontainers),
/// aplicando migraciones y seed. Permite probar los endpoints punta a punta.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _container.StartAsync();
        await Services.InitializeDatabaseAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Reemplaza el DbContext registrado por AddInfrastructure por uno apuntando al contenedor de
        // pruebas, conservando los interceptores de auditoría (ConfigurarPostgres).
        builder.ConfigureTestServices(services =>
        {
            // Quita TODO el registro del DbContext (incluida la acción de configuración de opciones,
            // IDbContextOptionsConfiguration); de lo contrario la configuración original de
            // AddInfrastructure se aplicaría además de esta y duplicaría los interceptores de auditoría.
            var descriptores = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<EventosDbContext>) ||
                d.ServiceType == typeof(EventosDbContext) ||
                d.ServiceType.Name.Contains("DbContextOptionsConfiguration")).ToList();
            foreach (var descriptor in descriptores)
                services.Remove(descriptor);

            services.AddDbContext<EventosDbContext>((sp, options) =>
                options.ConfigurarPostgres(_container.GetConnectionString(), sp));
        });
    }

    // Tokens cacheados por rol: se inicia sesión una sola vez (evita agotar el rate limit de login).
    private string? _tokenAdmin;
    private string? _tokenUsuario;

    private async Task<string> ObtenerTokenAsync(string usuario, string password)
    {
        var client = CreateClient();
        var respuesta = await client.PostAsJsonAsync("/api/v1/auth/login", new { nombreUsuario = usuario, password });
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private HttpClient ClienteConToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<HttpClient> CrearClienteAdminAsync() =>
        ClienteConToken(_tokenAdmin ??= await ObtenerTokenAsync("admin", "Admin123!"));

    public async Task<HttpClient> CrearClienteUsuarioAsync() =>
        ClienteConToken(_tokenUsuario ??= await ObtenerTokenAsync("usuario", "Usuario123!"));

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;
