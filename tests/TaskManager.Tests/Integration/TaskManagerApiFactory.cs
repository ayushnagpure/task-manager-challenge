using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Tests.Integration.TestSupport;

namespace TaskManager.Tests.Integration;

/// <summary>
/// Hosts the API in-memory under the "Testing" environment with explicit test configuration,
/// plus test-only endpoints (<see cref="TestErrorsController"/>) for exercising error handling.
/// </summary>
public sealed class TaskManagerApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:5173";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = AllowedOrigin,
            }));

        builder.ConfigureTestServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestErrorsController).Assembly));
    }
}
