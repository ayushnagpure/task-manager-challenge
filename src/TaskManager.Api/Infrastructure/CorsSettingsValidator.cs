using Microsoft.Extensions.Options;

namespace TaskManager.Api.Infrastructure;

/// <summary>
/// Validates <see cref="CorsSettings"/> at startup: every origin must be an exact http(s) origin,
/// wildcards are rejected, and outside Development at least one origin is required.
/// </summary>
public sealed class CorsSettingsValidator : IValidateOptions<CorsSettings>
{
    private readonly IHostEnvironment _environment;

    /// <summary>Creates the validator.</summary>
    public CorsSettingsValidator(IHostEnvironment environment) => _environment = environment;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CorsSettings options)
    {
        var failures = new List<string>();

        if (options.AllowedOrigins.Length == 0 && !_environment.IsDevelopment())
        {
            failures.Add($"{CorsSettings.SectionName}:AllowedOrigins must contain at least one origin outside Development.");
        }

        foreach (var origin in options.AllowedOrigins)
        {
            if (origin.Contains('*'))
            {
                failures.Add($"{CorsSettings.SectionName}:AllowedOrigins must not contain wildcards ('{origin}').");
            }
            else if (!IsExactOrigin(origin))
            {
                failures.Add($"{CorsSettings.SectionName}:AllowedOrigins entry '{origin}' is not a valid origin; expected scheme://host[:port] with no path or trailing slash.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsExactOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
}
