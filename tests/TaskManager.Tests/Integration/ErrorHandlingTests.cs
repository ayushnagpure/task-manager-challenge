using System.Net;
using System.Text.Json;
using TaskManager.Tests.Integration.TestSupport;

namespace TaskManager.Tests.Integration;

public sealed class ErrorHandlingTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetails_WithoutInternalDetails()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test/errors/unhandled");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(body);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred", json.RootElement.GetProperty("title").GetString());
        AssertHasTraceId(json);

        Assert.DoesNotContain(TestErrorsController.SensitiveMessage, body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("   at ", body); // stack frame marker
    }

    [Fact]
    public async Task NotFoundException_Returns404ProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test/errors/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Task with id '42' was not found.", json.RootElement.GetProperty("detail").GetString());
        AssertHasTraceId(json);
    }

    [Fact]
    public async Task UnknownRoute_Returns404ProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        AssertHasTraceId(json);
    }

    [Fact]
    public async Task WrongHttpMethod_Returns405ProblemDetails()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/test/errors/not-found", content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ErrorResponse_ToAllowedOrigin_IncludesCorsHeader()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/errors/unhandled");
        request.Headers.Add("Origin", TaskManagerApiFactory.AllowedOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(TaskManagerApiFactory.AllowedOrigin, Assert.Single(values));
    }

    private static void AssertHasTraceId(JsonDocument json)
    {
        Assert.True(json.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
