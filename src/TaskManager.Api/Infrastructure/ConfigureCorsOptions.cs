using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace TaskManager.Api.Infrastructure;

/// <summary>
/// Builds the UI CORS policy from validated <see cref="CorsSettings"/>. Resolved lazily so the
/// policy always reflects final configuration (including test overrides).
/// </summary>
public sealed class ConfigureCorsOptions : IConfigureOptions<CorsOptions>
{
    private static readonly string[] AllowedMethods = ["GET", "POST", "PUT", "PATCH", "DELETE"];
    private static readonly string[] AllowedHeaders = ["Content-Type"];

    private readonly IOptions<CorsSettings> _settings;

    /// <summary>Creates the configurator.</summary>
    public ConfigureCorsOptions(IOptions<CorsSettings> settings) => _settings = settings;

    /// <inheritdoc />
    public void Configure(CorsOptions options) =>
        options.AddPolicy(CorsSettings.PolicyName, policy => policy
            .WithOrigins(_settings.Value.AllowedOrigins)
            .WithMethods(AllowedMethods)
            .WithHeaders(AllowedHeaders));
}
