using System.Linq.Expressions;
using TaskManager.Api.Domain;

namespace TaskManager.Api.Services;

/// <summary>Reusable LINQ building blocks for task queries. Translated to SQL by EF Core.</summary>
public static class TaskQueries
{
    /// <summary>
    /// Priority rank used for sorting (Critical = 4 ... Low = 1). Enums are stored as strings, so
    /// <c>ORDER BY Priority</c> would sort alphabetically (Critical, High, Low, Medium).
    /// EF Core translates this expression to a SQL <c>CASE</c>.
    /// </summary>
    public static readonly Expression<Func<TaskItem, int>> PriorityRank = t =>
        t.Priority == TaskPriority.Critical ? 4 :
        t.Priority == TaskPriority.High ? 3 :
        t.Priority == TaskPriority.Medium ? 2 : 1;

    /// <summary>
    /// Applies the optional filters, then sorts by priority rank (Critical first), newest
    /// <see cref="TaskItem.CreatedDate"/> first, and finally by id so the order is deterministic.
    /// Soft-deleted tasks are already excluded by the global query filter.
    /// </summary>
    /// <param name="tasks">The source query.</param>
    /// <param name="status">Only tasks with this status, or all when null.</param>
    /// <param name="priority">Only tasks with this priority, or all when null.</param>
    public static IQueryable<TaskItem> FilterAndSort(this IQueryable<TaskItem> tasks, TaskItemStatus? status, TaskPriority? priority)
    {
        if (status is { } wantedStatus)
        {
            tasks = tasks.Where(t => t.Status == wantedStatus);
        }

        if (priority is { } wantedPriority)
        {
            tasks = tasks.Where(t => t.Priority == wantedPriority);
        }

        return tasks
            .OrderByDescending(PriorityRank)
            .ThenByDescending(t => t.CreatedDate)
            .ThenBy(t => t.Id);
    }
}
