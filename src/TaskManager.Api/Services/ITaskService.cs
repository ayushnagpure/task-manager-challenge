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

    /// <summary>
    /// Lists tasks, optionally filtered by status and/or priority, sorted by priority (Critical first)
    /// then newest first. <paramref name="query"/> must already have passed validation.
    /// </summary>
    /// <param name="query">The validated filters.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The matching tasks; empty when nothing matches.</returns>
    Task<IReadOnlyList<TaskResponse>> ListAsync(TaskListQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the editable fields of a task (full update) using optimistic concurrency.
    /// <paramref name="request"/> must already have passed validation.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="request">The validated request, including the client's rowVersion.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The updated task with its new row version.</returns>
    /// <exception cref="Domain.NotFoundException">No task with that id exists, or it is soft-deleted.</exception>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException">The task changed since the client read it.</exception>
    Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Changes only the status of a task. Last write wins (no rowVersion needed). Setting the status
    /// the task already has is a no-op: nothing is written and ModifiedDate/rowVersion stay the same.
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="request">The validated request.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The task, with its new row version when it changed.</returns>
    /// <exception cref="Domain.NotFoundException">No task with that id exists, or it is soft-deleted.</exception>
    Task<TaskResponse> UpdateStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a task: sets <c>IsDeleted</c> and <c>ModifiedDate</c>; the row is never removed.
    /// Last write wins (no rowVersion needed).
    /// </summary>
    /// <param name="id">The task id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="Domain.NotFoundException">No task with that id exists, or it is already deleted.</exception>
    Task DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Gets a task by id.</summary>
    /// <param name="id">The task id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <exception cref="Domain.NotFoundException">No task with that id exists, or it is soft-deleted.</exception>
    Task<TaskResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
