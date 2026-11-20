namespace TaskManager.Api.Domain;

/// <summary>
/// Thrown by services when a requested resource does not exist (or is soft-deleted).
/// Mapped to HTTP 404 by <see cref="Infrastructure.GlobalExceptionHandler"/>.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Creates a new <see cref="NotFoundException"/>.</summary>
    /// <param name="resourceName">Human-readable resource name, e.g. "Task".</param>
    /// <param name="key">The identifier that was looked up.</param>
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with id '{key}' was not found.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    /// <summary>Human-readable resource name, e.g. "Task".</summary>
    public string ResourceName { get; }

    /// <summary>The identifier that was looked up.</summary>
    public object Key { get; }
}
