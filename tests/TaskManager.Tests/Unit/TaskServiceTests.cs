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

    public void Dispose() => _db.Dispose();
}
