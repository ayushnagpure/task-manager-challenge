using TaskManager.Api.Contracts;

namespace TaskManager.Api.Services;

/// <summary>Reporting queries over tasks, implemented with raw SQL.</summary>
public interface ITaskSummaryService
{
    /// <summary>
    /// Counts active (not soft-deleted) tasks grouped by status and priority. Only combinations that
    /// have at least one task are returned, ordered Todo, InProgress, Done and then Critical, High, Medium, Low.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>One item per status/priority combination that has tasks; empty when there are none.</returns>
    Task<IReadOnlyList<TaskSummaryItem>> GetSummaryAsync(CancellationToken cancellationToken);
}
