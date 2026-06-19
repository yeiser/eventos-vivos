using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventosVivos.Infrastructure.Persistence;

/// <summary>
/// Factory de tiempo de diseño para que las herramientas de EF Core (migraciones) puedan crear el
/// contexto sin arrancar la API. La cadena de conexión se puede sobreescribir con EVENTOSVIVOS_DB.
/// </summary>
public sealed class EventosDbContextFactory : IDesignTimeDbContextFactory<EventosDbContext>
{
    public EventosDbContext CreateDbContext(string[] args)
    {
        var conexion = Environment.GetEnvironmentVariable("EVENTOSVIVOS_DB")
            ?? "Host=localhost;Port=5432;Database=eventosvivos;Username=eventosvivos;Password=eventosvivos_dev";

        var options = new DbContextOptionsBuilder<EventosDbContext>()
            .UseNpgsql(conexion)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new EventosDbContext(options);
    }
}
