using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventosVivos.Api.Swagger;

/// <summary>
/// Aplica el candado de seguridad (Bearer) <b>solo</b> a las operaciones protegidas. Las marcadas con
/// <c>[AllowAnonymous]</c> (p. ej. el login) quedan sin candado; el resto —protegidas por la política de
/// <i>fallback</i> o por <c>[Authorize]</c>— reciben el requisito de seguridad, las respuestas 401/403 y
/// una nota con el rol exigido.
/// </summary>
/// <remarks>
/// Es un <see cref="IDocumentFilter"/> (no un operation filter) porque la referencia al esquema de
/// seguridad necesita el <see cref="OpenApiDocument"/> para resolverse; sin él se serializa vacía y el
/// candado no aparece en Swagger UI.
/// </remarks>
public sealed partial class SecurityRequirementsOperationFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        foreach (var api in context.ApiDescriptions)
        {
            var metadata = api.ActionDescriptor.EndpointMetadata;

            // Endpoint público: sin candado.
            if (metadata.OfType<IAllowAnonymous>().Any())
                continue;

            var roles = metadata.OfType<IAuthorizeData>()
                .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                .SelectMany(a => a.Roles!.Split(','))
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .Distinct()
                .ToList();

            // Ruta del documento (sin las restricciones de ruta, p. ej. {id:guid} -> {id}).
            var path = "/" + RestriccionRuta().Replace(api.RelativePath ?? string.Empty, "{$1}");
            if (!document.Paths.TryGetValue(path, out var item) || item.Operations is null)
                continue;

            foreach (var entry in item.Operations)
            {
                if (!string.Equals(entry.Key.ToString(), api.HttpMethod, StringComparison.OrdinalIgnoreCase))
                    continue;

                AplicarSeguridad(entry.Value, document, roles);
            }
        }
    }

    private static void AplicarSeguridad(OpenApiOperation operation, OpenApiDocument document, List<string> roles)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses["401"] = new OpenApiResponse { Description = "No autenticado: falta el token o no es válido." };
        if (roles.Count > 0)
            operation.Responses["403"] = new OpenApiResponse { Description = $"Autenticado pero sin permisos (requiere rol {string.Join(" o ", roles)})." };

        var nota = roles.Count > 0
            ? $"🔒 Requiere autenticación y rol **{string.Join(" / ", roles)}**."
            : "🔒 Requiere autenticación.";
        operation.Description = string.IsNullOrWhiteSpace(operation.Description) ? nota : $"{operation.Description}\n\n{nota}";

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
            },
        ];
    }

    [GeneratedRegex(@"\{([^:}]+):[^}]+\}")]
    private static partial Regex RestriccionRuta();
}
