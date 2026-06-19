using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Errores;

/// <summary>Escribe respuestas ProblemDetails (RFC 7807) para 401/403 del middleware de autenticación.</summary>
internal static class RespuestaProblema
{
    public static async Task EscribirAsync(HttpContext context, int status, string tipo, string titulo, string detalle)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = status;

        var problema = new ProblemDetails
        {
            Status = status,
            Type = $"https://eventosvivos/errors/{tipo}",
            Title = titulo,
            Detail = detalle
        };
        problema.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(problema, problema.GetType(), options: null, contentType: "application/problem+json");
    }
}
