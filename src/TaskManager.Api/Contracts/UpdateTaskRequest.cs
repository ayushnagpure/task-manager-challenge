using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Contracts;

/// <summary>
/// Request body for <c>PUT /api/tasks/{id}</c>: a full replacement of the editable fields.
/// Omitted optional fields (description, assignedTo) are cleared. Id, dates and the deleted flag
/// cannot be changed and are ignored if sent.
/// </summary>
public sealed class UpdateTaskRequest : TaskFieldsRequest
{
    /// <summary>Required status: Todo, InProgress or Done (case-insensitive).</summary>
    [Required(ErrorMessage = "Status is required.")]
    [EnumName(typeof(TaskItemStatus))]
    public string? Status { get; init; }

    /// <summary>
    /// The <c>rowVersion</c> from the last GET/POST/PUT/PATCH response, unchanged. If the task has been
    /// changed since, the update is rejected with 409 Conflict.
    /// </summary>
    [Required(ErrorMessage = "RowVersion is required.")]
    [RowVersion]
    public string? RowVersion { get; init; }
}
