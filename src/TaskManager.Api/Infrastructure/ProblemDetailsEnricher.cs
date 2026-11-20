using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskManager.Api.Infrastructure;

/// <summary>
/// Adds the fields every error response must carry (<c>traceId</c>, <c>instance</c>) so all
/// ProblemDetails, whether from validation, exceptions or status codes, have the same shape.
/// </summary>
public static class ProblemDetailsEnricher
{
    /// <summary>Name of the extension member that carries the request trace id.</summary>
    public const string TraceIdKey = "traceId";

    /// <summary>Adds <c>traceId</c> and <c>instance</c> to <paramref name="problemDetails"/> when missing.</summary>
    /// <param name="problemDetails">The problem details being written.</param>
    /// <param name="httpContext">The current request.</param>
    public static void Enrich(ProblemDetails problemDetails, HttpContext httpContext)
    {
        problemDetails.Instance ??= $"{httpContext.Request.Method} {httpContext.Request.Path}";
        problemDetails.Extensions.TryAdd(TraceIdKey, GetTraceId(httpContext));
    }

    /// <summary>Returns the W3C trace id of the current activity, or the ASP.NET Core request id.</summary>
    /// <param name="httpContext">The current request.</param>
    public static string GetTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;
}
