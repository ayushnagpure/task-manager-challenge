namespace TaskManager.Api.Domain;

/// <summary>
/// Importance of a task. Stored as a string in the database, so ordering by priority must use an
/// explicit rank (CASE), not the column value, which would sort alphabetically.
/// </summary>
public enum TaskPriority
{
    /// <summary>Can wait.</summary>
    Low,

    /// <summary>Normal priority.</summary>
    Medium,

    /// <summary>Should be done soon.</summary>
    High,

    /// <summary>Must be done immediately.</summary>
    Critical,
}
