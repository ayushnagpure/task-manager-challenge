using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks</c>.</summary>
public sealed class CreateTaskRequest : TaskFieldsRequest
{
    /// <summary>Optional status: Todo, InProgress or Done (case-insensitive). Defaults to Todo.</summary>
    [EnumName(typeof(TaskItemStatus))]
    public string? Status { get; init; }
}
