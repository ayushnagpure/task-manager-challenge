using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TaskManager.Api.Infrastructure;

namespace TaskManager.Tests.Integration;

public sealed class StartupValidationTests
{
    [Fact]
    public async Task Startup_InProductionWithoutCorsOrigins_Fails()
    {
        // Production reads only appsettings.json, where Cors:AllowedOrigins is empty.
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("Cors:AllowedOrigins must contain at least one origin", exception.ToString());
    }

    [Fact]
    public async Task Startup_OutsideDevelopmentWithoutConnectionString_Fails()
    {
        // Staging reads only appsettings.json (no connection string); CORS is made valid so the
        // connection string is the only problem.
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Staging");
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://tasks.example.com" }));
            });

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains(DatabaseSettings.MissingConnectionStringMessage, exception.ToString());
    }

    [Fact]
    public async Task Startup_WithWildcardCorsOrigin_Fails()
    {
        await using var factory = new TaskManagerApiFactory()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "*" })));

        var exception = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("must not contain wildcards", exception.ToString());
    }
}
