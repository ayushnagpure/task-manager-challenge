using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Data.Configurations;
using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Contracts;

/// <summary>Editable task fields shared by create and full update, with their validation rules.</summary>
public abstract class TaskFieldsRequest
{
    /// <summary>Short title. Required, not blank, max 200 characters. Leading/trailing whitespace is trimmed.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(TaskItemConfiguration.TitleMaxLength, ErrorMessage = "Title must be at most {1} characters.")]
    public string? Title { get; init; }

    /// <summary>Optional description, max 2000 characters. Blank becomes null.</summary>
    [StringLength(TaskItemConfiguration.DescriptionMaxLength, ErrorMessage = "Description must be at most {1} characters.")]
    public string? Description { get; init; }

    /// <summary>Required priority: Low, Medium, High or Critical (case-insensitive).</summary>
    [Required(ErrorMessage = "Priority is required.")]
    [EnumName(typeof(TaskPriority))]
    public string? Priority { get; init; }

    /// <summary>Optional assignee name, max 100 characters. Blank becomes null.</summary>
    [StringLength(TaskItemConfiguration.AssignedToMaxLength, ErrorMessage = "AssignedTo must be at most {1} characters.")]
    public string? AssignedTo { get; init; }
}
