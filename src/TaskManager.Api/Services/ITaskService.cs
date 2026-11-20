using TaskManager.Api.Contracts;

namespace TaskManager.Api.Services;

/// <summary>Task use cases. Soft-deleted tasks are treated as non-existent.</summary>
public interface ITaskService
{
    /// <summary>Creates a task. <paramref name="request"/> must already have passed validation.</summary>
    /// <param name="request">The validated request.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The created task, including its id and row version.</returns>
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    /// <summary>Gets a task by id.</summary>
    /// <param name="id">The task id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="Domain.NotFoundException">No task with that id exists, or it is soft-deleted.</exception>
    Task<TaskResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
