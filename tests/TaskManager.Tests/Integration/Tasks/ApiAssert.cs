using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManager.Tests.Integration.Tasks;

/// <summary>Shared assertions for API responses.</summary>
internal static class ApiAssert
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Asserts a 400 ValidationProblemDetails with an error under exactly <paramref name="errorKey"/>
    /// (ordinal, case-sensitive, so "Title" does not satisfy "title"). Returns the parsed body.
    /// </summary>
    public static async Task<JsonElement> ValidationErrorAsync(HttpResponseMessage response, string errorKey)
    {
        var root = await ProblemAsync(response, HttpStatusCode.BadRequest);
        var errors = root.GetProperty("errors");
        Assert.True(
            errors.EnumerateObject().Any(e => e.Name == errorKey),
            $"Expected a validation error under key '{errorKey}', got: {errors}");
        return root;
    }

    /// <summary>Returns the first message for <paramref name="errorKey"/> in a ValidationProblemDetails body.</summary>
    public static string? FirstError(JsonElement problem, string errorKey) =>
        problem.GetProperty("errors").GetProperty(errorKey)[0].GetString();

    /// <summary>Returns the error keys of a ValidationProblemDetails body, sorted.</summary>
    public static string[] ErrorKeys(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Order(StringComparer.Ordinal).ToArray();

    /// <summary>Asserts a problem+json response with the given status and a traceId. Returns the parsed body.</summary>
    public static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement.Clone();
        Assert.Equal((int)status, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        return root;
    }
}
