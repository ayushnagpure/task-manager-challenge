using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Domain;

namespace TaskManager.Api.Infrastructure;

/// <summary>
/// Converts every unhandled exception into an RFC 7807 ProblemDetails response.
/// Known exceptions map to specific 4xx codes; anything else becomes a generic 500.
/// Exception messages and stack traces are logged server-side only and never sent to the client
/// (except for <see cref="NotFoundException"/>, whose message is composed by our own code).
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>Creates the handler.</summary>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = ProblemDetailsEnricher.GetTraceId(httpContext);
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value;

        // The client went away; there is nobody to send a response to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client. TraceId {TraceId}", method, path, traceId);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var problem = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = notFound.Message,
            },
            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "The task was changed by someone else. Reload it and try again.",
            },
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Bad request",
                Detail = "The request could not be processed.",
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = "An unexpected error occurred. Quote the traceId when reporting this problem.",
            },
        };

        var status = problem.Status!.Value;
        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}. TraceId {TraceId}", method, path, traceId);
        }
        else
        {
            _logger.LogWarning("Request {Method} {Path} failed with {StatusCode} ({ExceptionType}). TraceId {TraceId}",
                method, path, status, exception.GetType().Name, traceId);
        }

        httpContext.Response.StatusCode = status;

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });

        if (!written)
        {
            // The client's Accept header ruled out JSON; still send the standard shape.
            ProblemDetailsEnricher.Enrich(problem, httpContext);
            await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        }

        return true;
    }
}
