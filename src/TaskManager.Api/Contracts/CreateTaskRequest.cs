using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Data.Configurations;
using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks</c>.</summary>
public sealed class CreateTaskRequest
{
    /// <summary>Short title. Required, not blank, max 200 characters. Leading/trailing whitespace is trimmed.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(TaskItemConfiguration.TitleMaxLength, ErrorMessage = "Title must be at most {1} characters.")]
    public string? Title { get; init; }

    /// <summary>Optional description, max 2000 characters.</summary>
    [StringLength(TaskItemConfiguration.DescriptionMaxLength, ErrorMessage = "Description must be at most {1} characters.")]
    public string? Description { get; init; }

    /// <summary>Optional status: Todo, InProgress or Done (case-insensitive). Defaults to Todo.</summary>
    [EnumName(typeof(TaskItemStatus))]
    public string? Status { get; init; }

    /// <summary>Required priority: Low, Medium, High or Critical (case-insensitive).</summary>
    [Required(ErrorMessage = "Priority is required.")]
    [EnumName(typeof(TaskPriority))]
    public string? Priority { get; init; }

    /// <summary>Optional assignee name, max 100 characters.</summary>
    [StringLength(TaskItemConfiguration.AssignedToMaxLength, ErrorMessage = "AssignedTo must be at most {1} characters.")]
    public string? AssignedTo { get; init; }
}
