using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuscaYa.Utils;

/// <summary>
/// Manejador global de excepciones nativo de .NET 9 (IExceptionHandler).
/// Intercepta errores no controlados, genera logs estructurados y retorna
/// respuestas JSON estandarizadas (RFC 7807 Problem Details) para clientes móviles y API,
/// evitando que la app móvil reciba páginas HTML de error que ocasionen crasheos.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Excepción no controlada en {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        // Si la respuesta ya comenzó a escribirse, delegar al runtime
        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("La respuesta ya había comenzado al capturar la excepción.");
            return false;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        var isApiRequest = path.StartsWith("/api", StringComparison.OrdinalIgnoreCase);

        // Si es una petición MVC tradicional del navegador, permitir que el pipeline web estándar maneje la vista
        if (!isApiRequest && !httpContext.Request.Headers.Accept.Any(a => a != null && a.Contains("application/json")))
        {
            return false;
        }

        var (statusCode, title, defaultMessage) = exception switch
        {
            BadHttpRequestException badReq => (StatusCodes.Status400BadRequest, "Petición inválida", badReq.Message),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, "Datos inválidos", argEx.Message),
            UnauthorizedAccessException unauthEx => (StatusCodes.Status401Unauthorized, "No autorizado", unauthEx.Message),
            KeyNotFoundException notFoundEx => (StatusCodes.Status404NotFound, "Recurso no encontrado", notFoundEx.Message),
            InvalidOperationException invEx => (StatusCodes.Status409Conflict, "Conflicto de operación", invEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor", "Ha ocurrido un error inesperado al procesar la solicitud.")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        // En desarrollo se puede exponer más detalle; en producción se protege la información sensible
        var clientMessage = _env.IsDevelopment() ? exception.Message : defaultMessage;

        var responsePayload = new
        {
            status = statusCode,
            title = title,
            error = clientMessage,
            errorMessages = new[] { clientMessage },
            path = path,
            timestamp = DateTime.UtcNow
        };

        await httpContext.Response.WriteAsJsonAsync(responsePayload, cancellationToken);
        return true;
    }
}

