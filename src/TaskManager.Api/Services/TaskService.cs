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
    public async Task<IReadOnlyList<TaskResponse>> ListAsync(TaskListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        TaskItemStatus? status = query.Status is null ? null : EnumNameAttribute.Parse<TaskItemStatus>(query.Status);
        TaskPriority? priority = query.Priority is null ? null : EnumNameAttribute.Parse<TaskPriority>(query.Priority);

        var tasks = await _db.Tasks
            .AsNoTracking()
            .FilterAndSort(status, priority)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Listed {Count} tasks (status {Status}, priority {Priority})", tasks.Count, status, priority);
        return tasks.Select(t => t.ToResponse()).ToList();
    }

    /// <inheritdoc />
    public async Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = await FindTrackedAsync(id, cancellationToken);

        // Optimistic concurrency: EF compares the ORIGINAL value of the concurrency token in the
        // UPDATE's WHERE clause. Setting it to the client's rowVersion means the update only succeeds
        // if nobody has changed the row since the client read it; otherwise SaveChanges throws
        // DbUpdateConcurrencyException, which GlobalExceptionHandler maps to 409.
        _db.Entry(task).Property(t => t.RowVersion).OriginalValue = RowVersionAttribute.Decode(request.RowVersion!);

        task.Title = request.Title!.Trim();
        task.Description = NullIfBlank(request.Description);
        task.Status = EnumNameAttribute.Parse<TaskItemStatus>(request.Status!);
        task.Priority = EnumNameAttribute.Parse<TaskPriority>(request.Priority!);
        task.AssignedTo = NullIfBlank(request.AssignedTo);
        task.ModifiedDate = _timeProvider.GetUtcNow(); // always changes, so an UPDATE (and the concurrency check) always runs
        // CreatedDate, IsDeleted and Id are never touched.

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated task {TaskId}", task.Id);
        return task.ToResponse();
    }

    /// <inheritdoc />
    public async Task<TaskResponse> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = await FindTrackedAsync(id, cancellationToken);
        var newStatus = EnumNameAttribute.Parse<TaskItemStatus>(request.Status!);

        if (task.Status == newStatus)
        {
            return task.ToResponse(); // no-op: nothing written, ModifiedDate and rowVersion unchanged
        }

        // Last write wins: the client sends no rowVersion. The only possible conflict is a write that
        // lands between our load and save, which EF reports as DbUpdateConcurrencyException (409).
        var oldStatus = task.Status;
        task.Status = newStatus;
        task.ModifiedDate = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Changed status of task {TaskId} from {OldStatus} to {NewStatus}", task.Id, oldStatus, newStatus);
        return task.ToResponse();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        // Unknown and already-deleted ids both fail here (global query filter), so a second delete is 404.
        var task = await FindTrackedAsync(id, cancellationToken);

        // Soft delete: the row stays; the global query filter hides it from every query from now on.
        // ModifiedDate records when it was deleted (a deleted task can't be edited afterwards).
        task.IsDeleted = true;
        task.ModifiedDate = _timeProvider.GetUtcNow();

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Soft-deleted task {TaskId}", task.Id);
    }

    /// <inheritdoc />
    public async Task<TaskResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var task = await _db.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(ResourceName, id);

        return task.ToResponse();
    }

    // Tracked load through the global query filter: unknown and soft-deleted ids are both "not found".
    private async Task<TaskItem> FindTrackedAsync(int id, CancellationToken cancellationToken) =>
        await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException(ResourceName, id);

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
