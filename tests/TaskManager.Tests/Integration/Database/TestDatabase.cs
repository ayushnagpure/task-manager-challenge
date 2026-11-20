using Microsoft.Data.SqlClient;

namespace TaskManager.Tests.Integration.Database;

/// <summary>
/// Connection details for the integration-test database. Tests wipe all data, so the database
/// name must end in "_Tests"; this guard makes it impossible to point tests at TaskManagerDb.
/// </summary>
public static class TestDatabase
{
    /// <summary>Environment variable that overrides the default test connection string.</summary>
    public const string ConnectionStringVariable = "TASKMANAGER_TEST_CONNECTION";

    private const string DefaultConnectionString =
        @"Server=localhost\SQLEXPRESS;Database=TaskManagerDb_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    public static string ConnectionString =>
        EnsureTestDatabase(Environment.GetEnvironmentVariable(ConnectionStringVariable) is { Length: > 0 } fromEnv
            ? fromEnv
            : DefaultConnectionString);

    public static string EnsureTestDatabase(string connectionString)
    {
        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        if (!database.EndsWith("_Tests", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run tests against database '{database}': tests delete all data, so the database name must end with '_Tests'.");
        }

        return connectionString;
    }
}
