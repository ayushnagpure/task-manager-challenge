namespace TaskManager.Api.Domain;

/// <summary>
/// Workflow state of a task. Stored as a string in the database.
/// Named <c>TaskItemStatus</c> to avoid clashing with <see cref="System.Threading.Tasks.TaskStatus"/>.
/// </summary>
public enum TaskItemStatus
{
    /// <summary>Not started.</summary>
    Todo,

    /// <summary>Being worked on.</summary>
    InProgress,

    /// <summary>Finished.</summary>
    Done,
}
