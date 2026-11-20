using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Domain;
using TaskManager.Api.Services;
using Xunit.Abstractions;

namespace TaskManager.Tests.Unit;

public sealed class TaskQueriesTests(ITestOutputHelper output)
{
    private static readonly DateTimeOffset Day1 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FilterAndSort_NoFilters_SortsByPriorityRankThenNewestFirst()
    {
        var tasks = new[]
        {
            NewTask(1, TaskPriority.Low, Day1),
            NewTask(2, TaskPriority.Critical, Day1),
            NewTask(3, TaskPriority.Medium, Day1),
            NewTask(4, TaskPriority.High, Day1),
            NewTask(5, TaskPriority.High, Day1.AddDays(1)), // newer High comes before older High
        };

        var ids = tasks.AsQueryable().FilterAndSort(status: null, priority: null).Select(t => t.Id).ToList();

        Assert.Equal([2, 5, 4, 3, 1], ids);
    }

    [Fact]
    public void FilterAndSort_SameRankAndDate_FallsBackToId()
    {
        var tasks = new[] { NewTask(9, TaskPriority.Low, Day1), NewTask(7, TaskPriority.Low, Day1), NewTask(8, TaskPriority.Low, Day1) };

        var ids = tasks.AsQueryable().FilterAndSort(null, null).Select(t => t.Id).ToList();

        Assert.Equal([7, 8, 9], ids);
    }

    [Fact]
    public void FilterAndSort_AppliesBothFilters()
    {
        var tasks = new[]
        {
            NewTask(1, TaskPriority.High, Day1, TaskItemStatus.Todo),
            NewTask(2, TaskPriority.High, Day1, TaskItemStatus.Done),
            NewTask(3, TaskPriority.Low, Day1, TaskItemStatus.Todo),
        };

        var ids = tasks.AsQueryable().FilterAndSort(TaskItemStatus.Todo, TaskPriority.High).Select(t => t.Id).ToList();

        Assert.Equal([1], ids);
    }

    [Fact]
    public void FilterAndSort_TranslatesPrioritySortToSqlCase()
    {
        // No connection is opened: ToQueryString only generates SQL.
        using var db = new TaskDbContext(new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=Unused_Tests;Trusted_Connection=True")
            .Options);

        var sql = db.Tasks.FilterAndSort(TaskItemStatus.InProgress, TaskPriority.High).ToQueryString();
        output.WriteLine(sql);

        Assert.Contains("ORDER BY CASE", sql);
        Assert.Contains("WHEN [t].[Priority] = N'Critical' THEN 4", sql);
        Assert.DoesNotContain("ORDER BY [t].[Priority]", sql);
        Assert.Contains("[t].[IsDeleted] = CAST(0 AS bit)", sql); // global soft-delete filter
    }

    private static TaskItem NewTask(int id, TaskPriority priority, DateTimeOffset created, TaskItemStatus status = TaskItemStatus.Todo) =>
        new() { Id = id, Title = $"Task {id}", Priority = priority, Status = status, CreatedDate = created, ModifiedDate = created };
}
