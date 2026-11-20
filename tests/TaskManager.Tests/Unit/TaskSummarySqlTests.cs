using TaskManager.Api.Domain;
using TaskManager.Api.Services;

namespace TaskManager.Tests.Unit;

public sealed class TaskSummarySqlTests
{
    [Fact]
    public void SummarySql_ExcludesSoftDeletedRows()
    {
        // EF's global query filter does not apply to raw SQL, so the filter must be in the text.
        Assert.Contains("WHERE [IsDeleted] = 0", TaskSummaryService.SummarySql);
    }

    [Fact]
    public void SummarySql_RanksEveryStatusName()
    {
        // Drift guard: adding a TaskItemStatus value without ranking it in the ORDER BY fails here.
        Assert.All(Enum.GetNames<TaskItemStatus>(), name => Assert.Contains($"WHEN N'{name}' THEN", TaskSummaryService.SummarySql));
    }

    [Fact]
    public void SummarySql_RanksEveryPriorityName()
    {
        Assert.All(Enum.GetNames<TaskPriority>(), name => Assert.Contains($"WHEN N'{name}' THEN", TaskSummaryService.SummarySql));
    }

    [Fact]
    public void SummarySql_HasNoParametersOrPlaceholders()
    {
        // Constant text: nothing is interpolated or concatenated into it.
        Assert.DoesNotContain("{", TaskSummaryService.SummarySql);
        Assert.DoesNotContain("@", TaskSummaryService.SummarySql);
    }
}
