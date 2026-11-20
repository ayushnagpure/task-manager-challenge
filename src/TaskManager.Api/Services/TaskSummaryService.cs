using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Api.Infrastructure;

namespace TaskManager.Api.Services;

/// <summary>
/// <see cref="ITaskSummaryService"/> implemented with plain ADO.NET (no EF Core). Uses the same
/// <c>ConnectionStrings:DefaultConnection</c> as the rest of the app.
/// </summary>
public sealed class TaskSummaryService : ITaskSummaryService
{
    /// <summary>
    /// The summary query. A constant: no concatenation and no user input, so no parameters are needed;
    /// any future filter must be added as a typed <see cref="SqlParameter"/>.
    /// <para>
    /// <c>WHERE [IsDeleted] = 0</c> is required because EF Core's global soft-delete filter does not
    /// apply to raw SQL. The ORDER BY uses CASE ranks because the enums are stored as strings, so
    /// ordering by the columns would be alphabetical.
    /// </para>
    /// </summary>
    public const string SummarySql = """
        SELECT [Status], [Priority], COUNT(*) AS [Count]
        FROM [dbo].[Tasks]
        WHERE [IsDeleted] = 0
        GROUP BY [Status], [Priority]
        ORDER BY
            CASE [Status] WHEN N'Todo' THEN 1 WHEN N'InProgress' THEN 2 WHEN N'Done' THEN 3 ELSE 4 END,
            CASE [Priority] WHEN N'Critical' THEN 1 WHEN N'High' THEN 2 WHEN N'Medium' THEN 3 WHEN N'Low' THEN 4 ELSE 5 END;
        """;

    private readonly string _connectionString;
    private readonly ILogger<TaskSummaryService> _logger;

    /// <summary>Creates the service.</summary>
    public TaskSummaryService(IOptions<DatabaseSettings> databaseSettings, ILogger<TaskSummaryService> logger)
    {
        _connectionString = databaseSettings.Value.ConnectionString;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaskSummaryItem>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(SummarySql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var statusOrdinal = reader.GetOrdinal("Status");
        var priorityOrdinal = reader.GetOrdinal("Priority");
        var countOrdinal = reader.GetOrdinal("Count");

        var items = new List<TaskSummaryItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            // Values are the exact enum names written by EF (HasConversion<string>) and guarded by the
            // CK_Tasks_Status / CK_Tasks_Priority check constraints, so an exact-case parse is safe.
            // An unexpected value would throw and surface as a logged 500 rather than bad data.
            items.Add(new TaskSummaryItem(
                Enum.Parse<TaskItemStatus>(reader.GetString(statusOrdinal)),
                Enum.Parse<TaskPriority>(reader.GetString(priorityOrdinal)),
                reader.GetInt32(countOrdinal)));
        }

        _logger.LogDebug("Task summary returned {Rows} rows", items.Count);
        return items;
    }
}
