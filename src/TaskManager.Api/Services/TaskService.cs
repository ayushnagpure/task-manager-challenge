using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Contracts;
using TaskManager.Api.Data;
using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Services;

/// <summary>EF Core implementation of <see cref="ITaskService"/>.</summary>
public sealed class TaskService : ITaskService
{
    private const string ResourceName = "Task";

    private readonly TaskDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TaskService> _logger;

    /// <summary>Creates the service.</summary>
    public TaskService(TaskDbContext db, TimeProvider timeProvider, ILogger<TaskService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow();
        var task = new TaskItem
        {
            Title = request.Title!.Trim(),
            Description = NullIfBlank(request.Description),
            Status = request.Status is null ? TaskItemStatus.Todo : EnumNameAttribute.Parse<TaskItemStatus>(request.Status),
            Priority = EnumNameAttribute.Parse<TaskPriority>(request.Priority!),
            AssignedTo = NullIfBlank(request.AssignedTo),
            CreatedDate = now,
            ModifiedDate = now,
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created task {TaskId} with status {Status} and priority {Priority}", task.Id, task.Status, task.Priority);
        return task.ToResponse();
    }

    /// <inheritdoc />
    public async Task<TaskResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(ResourceName, id);

        return task.ToResponse();
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
