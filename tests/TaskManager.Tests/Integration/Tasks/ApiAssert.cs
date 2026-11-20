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
    /// Asserts a 400 ValidationProblemDetails with an error whose key matches <paramref name="errorKey"/>
    /// (case-insensitive, so "$.priority" matches "priority"). Returns the parsed body.
    /// </summary>
    public static async Task<JsonElement> ValidationErrorAsync(HttpResponseMessage response, string errorKey)
    {
        var root = await ProblemAsync(response, HttpStatusCode.BadRequest);
        var errors = root.GetProperty("errors");
        Assert.True(
            errors.EnumerateObject().Any(e => e.Name.Contains(errorKey, StringComparison.OrdinalIgnoreCase)),
            $"Expected a validation error for '{errorKey}', got: {errors}");
        return root;
    }

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
