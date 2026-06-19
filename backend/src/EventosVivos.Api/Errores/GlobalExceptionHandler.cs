using System.Diagnostics;
using EventosVivos.Application.Common;
using EventosVivos.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace EventosVivos.Api.Errores;

/// <summary>
/// Traduce las excepciones a respuestas RFC 7807 (application/problem+json) uniformes (§12).
/// Distingue errores de entrada (400), recurso inexistente (404), estado/concurrencia (409),
/// reglas de negocio (422) y errores no controlados (500, sin filtrar detalles internos).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string BaseTipo = "https://eventosvivos/errors/";

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = Mapear(exception);
        httpContext.Response.StatusCode = problem.Status!.Value;

        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Error no controlado: {Mensaje}", exception.Message);
        else
            _logger.LogWarning("Solicitud rechazada ({Status}): {Mensaje}", problem.Status, exception.Message);

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static ProblemDetails Mapear(Exception exception) => exception switch
    {
        ValidationException ve => Validacion(ve),

        CredencialesInvalidasException ci => Crear(
            StatusCodes.Status401Unauthorized, "credenciales-invalidas", "Credenciales inválidas", ci.Message),

        CuentaBloqueadaException cb => CuentaBloqueada(cb),

        RecursoNoEncontradoException nf => Crear(
            StatusCodes.Status404NotFound, "recurso-no-encontrado", "Recurso no encontrado", nf.Message),

        ReglaNegocioException rn => CrearConRegla(
            StatusCodes.Status422UnprocessableEntity, $"regla-negocio/{rn.Regla}", "Regla de negocio incumplida", rn.Message, rn.Regla),

        EstadoInvalidoException ei => Crear(
            StatusCodes.Status409Conflict, "estado-invalido", "Operación no válida para el estado actual", ei.Message),

        DatosInvalidosException di => Crear(
            StatusCodes.Status422UnprocessableEntity, "datos-invalidos", "Datos inválidos", di.Message),

        DbUpdateConcurrencyException => Crear(
            StatusCodes.Status409Conflict, "conflicto-concurrencia",
            "Conflicto de concurrencia", "La operación no se pudo completar por una modificación concurrente. Reintente."),

        _ => Crear(
            StatusCodes.Status500InternalServerError, "error-interno", "Error interno",
            "Ocurrió un error inesperado. Si persiste, contacte al administrador.")
    };

    private static ProblemDetails CuentaBloqueada(CuentaBloqueadaException ex)
    {
        var problem = Crear(StatusCodes.Status423Locked, "cuenta-bloqueada", "Cuenta bloqueada", ex.Message);
        problem.Extensions["bloqueadoHasta"] = ex.BloqueadoHasta;
        return problem;
    }

    private static ProblemDetails Validacion(ValidationException ve)
    {
        var problem = Crear(StatusCodes.Status400BadRequest, "validacion", "Error de validación",
            "Uno o más campos no son válidos.");

        problem.Extensions["errors"] = ve.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return problem;
    }

    private static ProblemDetails Crear(int status, string tipo, string titulo, string detalle) => new()
    {
        Status = status,
        Type = BaseTipo + tipo,
        Title = titulo,
        Detail = detalle
    };

    private static ProblemDetails CrearConRegla(int status, string tipo, string titulo, string detalle, string regla)
    {
        var problem = Crear(status, tipo, titulo, detalle);
        problem.Extensions["regla"] = regla;
        return problem;
    }
}
