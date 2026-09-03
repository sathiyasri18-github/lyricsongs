using System.Net;
using System.Text.Json;
using Lyricsongs.Domain.VideoPlan;
using Microsoft.AspNetCore.Diagnostics;

namespace Lyricsongs.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            InvalidOperationException invalid => (HttpStatusCode.BadRequest, "Invalid request", invalid.Message),
            VideoPlanException plan => (HttpStatusCode.UnprocessableEntity, "Video plan validation failed", plan.Message),
            FileNotFoundException notFound => (HttpStatusCode.NotFound, "Not found", notFound.Message),
            _ => (HttpStatusCode.InternalServerError, "Server error", "An unexpected error occurred.")
        };

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
            detail = exception.Message;
        }
        else
        {
            _logger.LogWarning(exception, "{Title}: {Detail}", title, detail);
        }

        httpContext.Response.StatusCode = (int)status;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)status}",
            title,
            status = (int)status,
            detail,
            traceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            cancellationToken);

        return true;
    }
}
