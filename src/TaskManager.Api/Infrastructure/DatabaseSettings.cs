namespace TaskManager.Api.Infrastructure;

/// <summary>
/// Database connection settings, populated from <c>ConnectionStrings:DefaultConnection</c>
/// and validated at startup.
/// </summary>
public sealed class DatabaseSettings
{
    /// <summary>Name of the connection string in the <c>ConnectionStrings</c> section.</summary>
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>Error reported at startup when the connection string is missing.</summary>
    public const string MissingConnectionStringMessage =
        "ConnectionStrings:DefaultConnection is missing. Set it in appsettings.Development.json (local dev), " +
        "dotnet user-secrets, or the ConnectionStrings__DefaultConnection environment variable.";

    /// <summary>SQL Server connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
