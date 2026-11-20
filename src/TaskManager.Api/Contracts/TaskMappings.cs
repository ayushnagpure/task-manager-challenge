using TaskManager.Api.Domain;

namespace TaskManager.Api.Contracts;

/// <summary>Maps between the <see cref="TaskItem"/> entity and API contracts.</summary>
public static class TaskMappings
{
    /// <summary>Creates the API representation of a task.</summary>
    public static TaskResponse ToResponse(this TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssignedTo,
            task.CreatedDate,
            task.ModifiedDate,
            task.RowVersion);
}
