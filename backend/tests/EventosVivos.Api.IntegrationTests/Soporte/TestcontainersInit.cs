using System.Runtime.CompilerServices;

namespace EventosVivos.Api.IntegrationTests.Soporte;

internal static class TestcontainersInit
{
    /// <summary>
    /// El motor de Docker instalado (20.10.x) soporta como máximo la API 1.41, pero el cliente de
    /// Testcontainers solicita una más nueva. Fijamos la versión de API antes de crear cualquier
    /// contenedor para evitar el error "client version is too new". (Quitar al actualizar Docker.)
    /// </summary>
    [ModuleInitializer]
    public static void Init()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_API_VERSION")))
            Environment.SetEnvironmentVariable("DOCKER_API_VERSION", "1.41");
    }
}
