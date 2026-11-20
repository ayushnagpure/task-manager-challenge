using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TaskManager.Api.Infrastructure;

namespace TaskManager.Tests.Unit;

public sealed class CorsSettingsValidatorTests
{
    [Fact]
    public void Validate_EmptyOriginsInDevelopment_Succeeds()
    {
        var result = Validate(Environments.Development);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyOriginsInProduction_Fails()
    {
        var result = Validate(Environments.Production);

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("https://tasks.example.com")]
    [InlineData("https://tasks.example.com:8443")]
    public void Validate_ExactOrigin_Succeeds(string origin)
    {
        var result = Validate(Environments.Production, origin);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData("http://localhost:5173/")]
    [InlineData("http://localhost:5173/app")]
    [InlineData("ftp://localhost")]
    [InlineData("localhost:5173")]
    [InlineData("not a url")]
    public void Validate_InvalidOrigin_Fails(string origin)
    {
        var result = Validate(Environments.Development, origin);

        Assert.True(result.Failed);
    }

    private static Microsoft.Extensions.Options.ValidateOptionsResult Validate(string environmentName, params string[] origins) =>
        new CorsSettingsValidator(new StubHostEnvironment(environmentName))
            .Validate(name: null, new CorsSettings { AllowedOrigins = origins });

    private sealed class StubHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "TaskManager.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
