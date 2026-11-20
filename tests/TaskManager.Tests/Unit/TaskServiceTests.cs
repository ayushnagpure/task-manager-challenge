using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using TaskManager.Api.Contracts;
using TaskManager.Api.Data;
using TaskManager.Api.Domain;
using TaskManager.Api.Services;

namespace TaskManager.Tests.Unit;

/// <summary>
/// Service logic only. EF InMemory stands in for SQL Server; SQL behaviour (rowversion,
/// constraints, string enums) is covered by the integration tests.
/// </summary>
public sealed class TaskServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly TaskDbContext _db = new(new DbContextOptionsBuilder<TaskDbContext>()
        .UseInMemoryDatabase($"TaskServiceTests-{Guid.NewGuid()}")
        .Options);

    private readonly FakeTimeProvider _time = new(Now);

    private TaskService CreateService() => new(_db, _time, NullLogger<TaskService>.Instance);

    [Fact]
    public async Task CreateAsync_SetsCreatedAndModifiedDate_ToCurrentUtcTime()
    {
        var created = await CreateService().CreateAsync(
            new CreateTaskRequest { Title = "Plan sprint", Priority = "High" }, CancellationToken.None);

        Assert.Equal(Now, created.CreatedDate);
        Assert.Equal(created.CreatedDate, created.ModifiedDate);
        Assert.Equal(TimeSpan.Zero, created.CreatedDate.Offset);
    }

    [Fact]
    public async Task CreateAsync_TrimsText_AndDefaultsStatusToTodo()
    {
        var created = await CreateService().CreateAsync(
            new CreateTaskRequest { Title = "  Plan sprint  ", Description = "   ", Priority = "critical", AssignedTo = " alice " },
            CancellationToken.None);

        Assert.Equal("Plan sprint", created.Title);
        Assert.Null(created.Description);
        Assert.Equal("alice", created.AssignedTo);
        Assert.Equal(TaskItemStatus.Todo, created.Status);
        Assert.Equal(TaskPriority.Critical, created.Priority);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTask_ReturnsIt()
    {
        var created = await CreateService().CreateAsync(
            new CreateTaskRequest { Title = "Find me", Status = "InProgress", Priority = "Low" }, CancellationToken.None);

        var loaded = await CreateService().GetByIdAsync(created.Id, CancellationToken.None);

        Assert.Equal(created.Id, loaded.Id);
        Assert.Equal("Find me", loaded.Title);
        Assert.Equal(TaskItemStatus.InProgress, loaded.Status);
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedTask_ThrowsNotFound()
    {
        _db.Tasks.Add(new TaskItem
        {
            Id = 42,
            Title = "Gone",
            Priority = TaskPriority.Low,
            CreatedDate = Now,
            ModifiedDate = Now,
            IsDeleted = true,
        });
        await _db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(42, CancellationToken.None));
        Assert.Equal(42, exception.Key);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(999, CancellationToken.None));
    }

    // ---------- UpdateAsync (PUT) ----------
    // InMemory doesn't generate rowversions, so these tests store a known 8-byte value themselves.

    private static readonly byte[] StoredRowVersion = [0, 0, 0, 0, 0, 0, 0, 1];

    [Fact]
    public async Task UpdateAsync_SetsModifiedDateToNow_AndKeepsCreatedDate()
    {
        await SeedAsync(new TaskItem
        {
            Id = 7,
            Title = "Before",
            Status = TaskItemStatus.Todo,
            Priority = TaskPriority.Low,
            CreatedDate = Now,
            ModifiedDate = Now,
            RowVersion = StoredRowVersion,
        });
        _time.Advance(TimeSpan.FromHours(2));

        var updated = await CreateService().UpdateAsync(7, new UpdateTaskRequest
        {
            Title = "  After  ",
            Description = "New description",
            Status = "Done",
            Priority = "critical",
            AssignedTo = "Bob",
            RowVersion = Convert.ToBase64String(StoredRowVersion),
        }, CancellationToken.None);

        Assert.Equal(Now, updated.CreatedDate);
        Assert.Equal(Now.AddHours(2), updated.ModifiedDate);
        Assert.Equal("After", updated.Title);
        Assert.Equal("New description", updated.Description);
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.Equal(TaskPriority.Critical, updated.Priority);
        Assert.Equal("Bob", updated.AssignedTo);
    }

    [Fact]
    public async Task UpdateAsync_OmittedOptionalFields_AreCleared()
    {
        await SeedAsync(new TaskItem
        {
            Id = 8,
            Title = "Has extras",
            Description = "Old description",
            AssignedTo = "Alice",
            Priority = TaskPriority.Low,
            CreatedDate = Now,
            ModifiedDate = Now,
            RowVersion = StoredRowVersion,
        });

        var updated = await CreateService().UpdateAsync(8, new UpdateTaskRequest
        {
            Title = "Has extras",
            Status = "Todo",
            Priority = "Low",
            RowVersion = Convert.ToBase64String(StoredRowVersion),
        }, CancellationToken.None);

        Assert.Null(updated.Description);
        Assert.Null(updated.AssignedTo);
    }

    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ThrowsConcurrencyException()
    {
        await SeedAsync(new TaskItem
        {
            Id = 9,
            Title = "Contended",
            Priority = TaskPriority.Low,
            CreatedDate = Now,
            ModifiedDate = Now,
            RowVersion = StoredRowVersion,
        });

        var stale = Convert.ToBase64String(new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 });

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => CreateService().UpdateAsync(9, new UpdateTaskRequest
        {
            Title = "Lost update",
            Status = "Todo",
            Priority = "Low",
            RowVersion = stale,
        }, CancellationToken.None));
    }

    [Theory]
    [InlineData(404, false)] // unknown id
    [InlineData(10, true)]   // soft-deleted id
    public async Task UpdateAsync_UnknownOrSoftDeleted_ThrowsNotFound(int id, bool seedDeleted)
    {
        if (seedDeleted)
        {
            await SeedAsync(new TaskItem
            {
                Id = id,
                Title = "Gone",
                Priority = TaskPriority.Low,
                CreatedDate = Now,
                ModifiedDate = Now,
                IsDeleted = true,
                RowVersion = StoredRowVersion,
            });
        }

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService().UpdateAsync(id, new UpdateTaskRequest
        {
            Title = "x",
            Status = "Todo",
            Priority = "Low",
            RowVersion = Convert.ToBase64String(StoredRowVersion),
        }, CancellationToken.None));
    }

    // ---------- UpdateStatusAsync (PATCH) ----------

    [Fact]
    public async Task UpdateStatusAsync_SetsModifiedDate_AndKeepsOtherFields()
    {
        await SeedAsync(new TaskItem
        {
            Id = 20,
            Title = "Toggle me",
            Description = "Keep",
            AssignedTo = "Chen",
            Status = TaskItemStatus.Todo,
            Priority = TaskPriority.High,
            CreatedDate = Now,
            ModifiedDate = Now,
            RowVersion = StoredRowVersion,
        });
        _time.Advance(TimeSpan.FromMinutes(30));

        var updated = await CreateService().UpdateStatusAsync(20, new UpdateTaskStatusRequest { Status = "inprogress" }, CancellationToken.None);

        Assert.Equal(TaskItemStatus.InProgress, updated.Status);
        Assert.Equal(Now.AddMinutes(30), updated.ModifiedDate);
        Assert.Equal(Now, updated.CreatedDate);
        Assert.Equal("Toggle me", updated.Title);
        Assert.Equal("Keep", updated.Description);
        Assert.Equal("Chen", updated.AssignedTo);
        Assert.Equal(TaskPriority.High, updated.Priority);
    }

    [Fact]
    public async Task UpdateStatusAsync_SameStatus_DoesNotTouchModifiedDate()
    {
        await SeedAsync(new TaskItem
        {
            Id = 21,
            Title = "Already done",
            Status = TaskItemStatus.Done,
            Priority = TaskPriority.Low,
            CreatedDate = Now,
            ModifiedDate = Now,
            RowVersion = StoredRowVersion,
        });
        _time.Advance(TimeSpan.FromHours(1));

        var result = await CreateService().UpdateStatusAsync(21, new UpdateTaskStatusRequest { Status = "Done" }, CancellationToken.None);

        Assert.Equal(Now, result.ModifiedDate);
        Assert.Equal(StoredRowVersion, result.RowVersion);
    }

    [Fact]
    public async Task UpdateStatusAsync_SoftDeleted_ThrowsNotFound()
    {
        await SeedAsync(new TaskItem
        {
            Id = 22,
            Title = "Gone",
            Priority = TaskPriority.Low,
            CreatedDate = Now,
            ModifiedDate = Now,
            IsDeleted = true,
            RowVersion = StoredRowVersion,
        });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService().UpdateStatusAsync(22, new UpdateTaskStatusRequest { Status = "Done" }, CancellationToken.None));
    }

    private async Task SeedAsync(TaskItem task)
    {
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear(); // the service should load the row itself, like a fresh request
    }

    public void Dispose() => _db.Dispose();
}
