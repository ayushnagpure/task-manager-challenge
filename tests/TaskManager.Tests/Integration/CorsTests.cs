using System.Net;

namespace TaskManager.Tests.Integration;

public sealed class CorsTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";

    [Fact]
    public async Task Preflight_FromAllowedOrigin_ReturnsAllowOriginAndMethods()
    {
        var response = await SendPreflightAsync(TaskManagerApiFactory.AllowedOrigin, "PATCH");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(TaskManagerApiFactory.AllowedOrigin, Assert.Single(response.Headers.GetValues(AllowOriginHeader)));
        Assert.Contains("PATCH", string.Join(',', response.Headers.GetValues("Access-Control-Allow-Methods")));
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_HasNoAllowOriginHeader()
    {
        var response = await SendPreflightAsync("http://evil.test", "GET");

        Assert.False(response.Headers.Contains(AllowOriginHeader));
    }

    [Fact]
    public async Task Preflight_WithDisallowedHeader_DoesNotAllowThatHeader()
    {
        // ASP.NET Core still echoes the origin, but omits the header from Allow-Headers;
        // the browser then blocks the actual request.
        var response = await SendPreflightAsync(TaskManagerApiFactory.AllowedOrigin, "GET", requestHeaders: "Authorization");

        var allowedHeaders = response.Headers.TryGetValues("Access-Control-Allow-Headers", out var values)
            ? string.Join(',', values)
            : string.Empty;
        Assert.DoesNotContain("Authorization", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_WithAllowedContentTypeHeader_AllowsIt()
    {
        var response = await SendPreflightAsync(TaskManagerApiFactory.AllowedOrigin, "POST", requestHeaders: "Content-Type");

        Assert.Contains("Content-Type", string.Join(',', response.Headers.GetValues("Access-Control-Allow-Headers")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SimpleRequest_FromAllowedOrigin_IncludesAllowOriginHeader()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", TaskManagerApiFactory.AllowedOrigin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TaskManagerApiFactory.AllowedOrigin, Assert.Single(response.Headers.GetValues(AllowOriginHeader)));
    }

    private async Task<HttpResponseMessage> SendPreflightAsync(string origin, string method, string? requestHeaders = null)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        if (requestHeaders is not null)
        {
            request.Headers.Add("Access-Control-Request-Headers", requestHeaders);
        }

        return await client.SendAsync(request);
    }
}
