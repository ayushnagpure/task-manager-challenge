using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Contracts;

/// <summary>
/// Request body for <c>PATCH /api/tasks/{id}/status</c>. Last write wins: no rowVersion is needed,
/// and the response carries the new one.
/// </summary>
public sealed class UpdateTaskStatusRequest
{
    /// <summary>Required new status: Todo, InProgress or Done (case-insensitive).</summary>
    [Required(ErrorMessage = "Status is required.")]
    [EnumName(typeof(TaskItemStatus))]
    public string? Status { get; init; }
}
