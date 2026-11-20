using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Respawn.Graph;
using TaskManager.Api.Data;

namespace TaskManager.Tests.Integration.Database;

/// <summary>
/// Shared once per test run (xUnit collection fixture): migrates the test database, then lets each
/// test reset it to empty with Respawn. The migrations history table is never touched.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private Respawner? _respawner;

    /// <summary>API host wired to the test database, for HTTP-level tests in later steps.</summary>
    public TaskManagerApiFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        await using var connection = new SqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
    }

    /// <summary>Deletes all rows from every table except <c>__EFMigrationsHistory</c>.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }

    /// <summary>Creates a new context on the test database (each call is an independent unit of work).</summary>
    public TaskDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TaskDbContext>().UseSqlServer(TestDatabase.ConnectionString).Options);

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}
