using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Domain;
using TaskManager.Api.Validation;

namespace TaskManager.Api.Contracts;

/// <summary>Query string for <c>GET /api/tasks</c>. Both filters are optional; an empty value means "no filter".</summary>
public sealed class TaskListQuery
{
    /// <summary>Only return tasks with this status: Todo, InProgress or Done (case-insensitive).</summary>
    [FromQuery(Name = "status")]
    [EnumName(typeof(TaskItemStatus))]
    public string? Status { get; init; }

    /// <summary>Only return tasks with this priority: Low, Medium, High or Critical (case-insensitive).</summary>
    [FromQuery(Name = "priority")]
    [EnumName(typeof(TaskPriority))]
    public string? Priority { get; init; }
}
