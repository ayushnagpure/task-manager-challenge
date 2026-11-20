using TaskManager.Api.Domain;

namespace TaskManager.Api.Contracts;

/// <summary>A task as returned by the API.</summary>
/// <param name="Id">Task id.</param>
/// <param name="Title">Short title.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Status">Workflow state (serialized as a string).</param>
/// <param name="Priority">Importance (serialized as a string).</param>
/// <param name="AssignedTo">Optional assignee.</param>
/// <param name="CreatedDate">When the task was created (UTC).</param>
/// <param name="ModifiedDate">When the task was last changed (UTC).</param>
/// <param name="RowVersion">Concurrency token (base64). Send it back unchanged when updating the task.</param>
public sealed record TaskResponse(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssignedTo,
    DateTimeOffset CreatedDate,
    DateTimeOffset ModifiedDate,
    byte[] RowVersion);
