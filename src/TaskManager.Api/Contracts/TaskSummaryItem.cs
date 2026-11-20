using TaskManager.Api.Domain;

namespace TaskManager.Api.Contracts;

/// <summary>Number of active (not deleted) tasks with a given status and priority.</summary>
/// <param name="Status">Workflow state (serialized as a string).</param>
/// <param name="Priority">Importance (serialized as a string).</param>
/// <param name="Count">Number of active tasks with this status and priority. Always at least 1.</param>
public sealed record TaskSummaryItem(TaskItemStatus Status, TaskPriority Priority, int Count);
