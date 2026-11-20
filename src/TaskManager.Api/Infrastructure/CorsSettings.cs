namespace TaskManager.Api.Infrastructure;

/// <summary>CORS configuration bound from the <c>Cors</c> configuration section.</summary>
public sealed class CorsSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Cors";

    /// <summary>Name of the CORS policy used by the UI.</summary>
    public const string PolicyName = "TaskManagerUi";

    /// <summary>
    /// Exact origins (scheme://host[:port], no trailing slash) allowed to call the API from a browser.
    /// Wildcards are rejected.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
