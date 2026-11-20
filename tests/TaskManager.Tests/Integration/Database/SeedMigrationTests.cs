using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaskManager.Api.Data;
using TaskManager.Api.Domain;

namespace TaskManager.Tests.Integration.Database;

/// <summary>
/// Respawn empties Tasks before every test, so the seed can't be observed "left over" from the
/// fixture's migration. Instead this test re-runs the SeedTasks migration itself: down to
/// InitialCreate (DeleteData, harmless on an empty table) and back up (InsertData).
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class SeedMigrationTests(DatabaseFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();

    // Leave the shared database empty for whatever runs next.
    public Task DisposeAsync() => fixture.ResetAsync();

    [Fact]
    public async Task SeedMigration_InsertsAllSeedTasks()
    {
        await ReapplySeedMigrationAsync();

        await using var db = fixture.CreateDbContext();
        var rows = await db.Tasks.IgnoreQueryFilters().OrderBy(t => t.Id).ToListAsync();

        Assert.Equal(Project(SeedData.Tasks.OrderBy(t => t.Id)), Project(rows));
        Assert.All(rows, r => Assert.NotEmpty(r.RowVersion));
    }

    [Fact]
    public async Task SeedMigration_SoftDeletedTasks_AreHiddenByQueryFilter()
    {
        await ReapplySeedMigrationAsync();

        await using var db = fixture.CreateDbContext();

        Assert.Equal(12, await db.Tasks.CountAsync());
        Assert.Equal(15, await db.Tasks.IgnoreQueryFilters().CountAsync());
        Assert.Equal([13, 14, 15], await db.Tasks.IgnoreQueryFilters().Where(t => t.IsDeleted).Select(t => t.Id).OrderBy(id => id).ToListAsync());
    }

    [Fact]
    public async Task AfterSeed_NewTask_GetsIdAboveSeedRange()
    {
        await ReapplySeedMigrationAsync();

        await using var db = fixture.CreateDbContext();
        var task = new TaskItem
        {
            Title = "Created after seeding",
            Status = TaskItemStatus.Todo,
            Priority = TaskPriority.Low,
            CreatedDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        Assert.True(task.Id > 15, $"Expected identity to continue after the seed, got {task.Id}.");
    }

    private async Task ReapplySeedMigrationAsync()
    {
        await using var db = fixture.CreateDbContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("InitialCreate"); // runs SeedTasks.Down
        await migrator.MigrateAsync();                // runs SeedTasks.Up
    }

    private static List<(int, string, string?, TaskItemStatus, TaskPriority, string?, DateTimeOffset, DateTimeOffset, bool)> Project(IEnumerable<TaskItem> tasks) =>
        tasks.Select(t => (t.Id, t.Title, t.Description, t.Status, t.Priority, t.AssignedTo, t.CreatedDate, t.ModifiedDate, t.IsDeleted)).ToList();
}
