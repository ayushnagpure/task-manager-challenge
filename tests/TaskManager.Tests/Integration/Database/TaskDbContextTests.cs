using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Domain;

namespace TaskManager.Tests.Integration.Database;

[Collection(DatabaseCollection.Name)]
public sealed class TaskDbContextTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Created = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SaveAndLoad_RoundTripsAllFields()
    {
        var task = NewTask("Write report", TaskItemStatus.InProgress, TaskPriority.Critical);
        task.Description = "Quarterly numbers";
        task.AssignedTo = "alice";
        task.ModifiedDate = Created.AddHours(2);
        await SaveAsync(task);

        await using var db = fixture.CreateDbContext();
        var loaded = await db.Tasks.SingleAsync(t => t.Id == task.Id);

        Assert.Equal("Write report", loaded.Title);
        Assert.Equal("Quarterly numbers", loaded.Description);
        Assert.Equal(TaskItemStatus.InProgress, loaded.Status);
        Assert.Equal(TaskPriority.Critical, loaded.Priority);
        Assert.Equal("alice", loaded.AssignedTo);
        Assert.Equal(Created, loaded.CreatedDate);
        Assert.Equal(Created.AddHours(2), loaded.ModifiedDate);
        Assert.False(loaded.IsDeleted);
    }

    [Fact]
    public async Task Enums_AreStoredAsStrings()
    {
        var task = NewTask("Enum storage", TaskItemStatus.InProgress, TaskPriority.Critical);
        await SaveAsync(task);

        await using var db = fixture.CreateDbContext();
        var status = await db.Database.SqlQuery<string>($"SELECT [Status] AS [Value] FROM [Tasks] WHERE [Id] = {task.Id}").SingleAsync();
        var priority = await db.Database.SqlQuery<string>($"SELECT [Priority] AS [Value] FROM [Tasks] WHERE [Id] = {task.Id}").SingleAsync();

        Assert.Equal("InProgress", status);
        Assert.Equal("Critical", priority);
    }

    [Fact]
    public async Task SoftDeleteFilter_HidesDeletedTasks_UnlessIgnored()
    {
        var active = NewTask("Active");
        var deleted = NewTask("Deleted");
        deleted.IsDeleted = true;
        await SaveAsync(active, deleted);

        await using var db = fixture.CreateDbContext();

        var visible = await db.Tasks.ToListAsync();
        Assert.Equal("Active", Assert.Single(visible).Title);
        Assert.Null(await db.Tasks.FirstOrDefaultAsync(t => t.Id == deleted.Id));
        Assert.Equal(2, await db.Tasks.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task RowVersion_IsSetOnInsert_AndChangesOnUpdate()
    {
        var task = NewTask("Versioned");
        await SaveAsync(task);
        var original = task.RowVersion;
        Assert.NotEmpty(original);

        await using var db = fixture.CreateDbContext();
        var loaded = await db.Tasks.SingleAsync(t => t.Id == task.Id);
        loaded.Title = "Versioned (edited)";
        await db.SaveChangesAsync();

        Assert.NotEqual(original, loaded.RowVersion);
    }

    [Fact]
    public async Task ConcurrentUpdate_WithStaleRowVersion_Throws()
    {
        var task = NewTask("Contended");
        await SaveAsync(task);

        await using var first = fixture.CreateDbContext();
        await using var second = fixture.CreateDbContext();
        var firstCopy = await first.Tasks.SingleAsync(t => t.Id == task.Id);
        var secondCopy = await second.Tasks.SingleAsync(t => t.Id == task.Id);

        firstCopy.Title = "First writer wins";
        await first.SaveChangesAsync();

        secondCopy.Title = "Second writer is stale";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Theory]
    [InlineData("Blocked", "Low", "CK_Tasks_Status")]
    [InlineData("Todo", "Urgent", "CK_Tasks_Priority")]
    public async Task CheckConstraints_RejectUnknownEnumValues(string status, string priority, string constraint)
    {
        await using var db = fixture.CreateDbContext();

        var exception = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO [Tasks] ([Title], [Status], [Priority], [CreatedDate], [ModifiedDate], [IsDeleted])
            VALUES (N'Bad enum', {status}, {priority}, SYSUTCDATETIME(), SYSUTCDATETIME(), 0)
            """));

        Assert.Contains(constraint, exception.Message);
    }

    private static TaskItem NewTask(string title, TaskItemStatus status = TaskItemStatus.Todo, TaskPriority priority = TaskPriority.Medium) =>
        new()
        {
            Title = title,
            Status = status,
            Priority = priority,
            CreatedDate = Created,
            ModifiedDate = Created,
        };

    private async Task SaveAsync(params TaskItem[] tasks)
    {
        await using var db = fixture.CreateDbContext();
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
    }
}
