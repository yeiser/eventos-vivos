using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EventosVivos.Api.Swagger;

/// <summary>
/// Aplica el candado de seguridad (Bearer) <b>solo</b> a las operaciones protegidas. Las marcadas con
/// <c>[AllowAnonymous]</c> (p. ej. el login) quedan sin candado; el resto —protegidas por la política de
/// <i>fallback</i> o por <c>[Authorize]</c>— reciben el requisito de seguridad, las respuestas 401/403 y
/// una nota con el rol exigido. Así el candado refleja la realidad en vez de aplicarse globalmente.
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // Endpoint público: sin candado ni respuestas de auth.
        if (metadata.OfType<IAllowAnonymous>().Any())
            return;

        var roles = metadata.OfType<IAuthorizeData>()
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(','))
            .Select(r => r.Trim())
            .Where(r => r.Length > 0)
            .Distinct()
            .ToList();

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
                [new OpenApiSecuritySchemeReference("Bearer", null)] = new List<string>(),
            },
        ];
    }
}
