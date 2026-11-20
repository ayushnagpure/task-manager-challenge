using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Api.Services;
using TaskManager.Tests.Integration.Database;

namespace TaskManager.Tests.Integration.Tasks;

[Collection(DatabaseCollection.Name)]
public sealed class SummaryTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private const string Url = "/api/tasks/summary";
    private static readonly DateTimeOffset Created = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Summary_EmptyDatabase_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Summary_OnlyDeletedTasks_ReturnsEmptyArray()
    {
        await InsertAsync((TaskItemStatus.Todo, TaskPriority.High, 2, true));

        Assert.Equal("[]", await _client.GetStringAsync(Url));
    }

    [Fact]
    public async Task Summary_KnownFixture_ReturnsExactCountsInLogicalOrder()
    {
        await InsertFixtureAsync();

        var summary = await GetSummaryAsync();

        // Alphabetical order would start with Done and put Low before Medium; this is the CASE order.
        Assert.Equal(
            [
                new TaskSummaryItem(TaskItemStatus.Todo, TaskPriority.High, 2),
                new TaskSummaryItem(TaskItemStatus.Todo, TaskPriority.Medium, 1),
                new TaskSummaryItem(TaskItemStatus.Todo, TaskPriority.Low, 1),
                new TaskSummaryItem(TaskItemStatus.InProgress, TaskPriority.Critical, 1),
                new TaskSummaryItem(TaskItemStatus.Done, TaskPriority.Critical, 3),
            ],
            summary);
    }

    [Fact]
    public async Task Summary_CombinationsWithNoActiveTasks_AreAbsent()
    {
        await InsertFixtureAsync();

        var summary = await GetSummaryAsync();

        Assert.Equal(5, summary.Count); // of 12 possible combinations
        Assert.DoesNotContain(summary, s => s.Status == TaskItemStatus.Done && s.Priority == TaskPriority.Low); // only deleted rows
        Assert.DoesNotContain(summary, s => s.Status == TaskItemStatus.InProgress && s.Priority == TaskPriority.Low); // no rows at all
        Assert.All(summary, s => Assert.True(s.Count > 0));
    }

    [Fact]
    public async Task Summary_ExcludesSoftDeletedTasks()
    {
        await InsertFixtureAsync();

        var todoHigh = Assert.Single(await GetSummaryAsync(), s => s.Status == TaskItemStatus.Todo && s.Priority == TaskPriority.High);

        Assert.Equal(2, todoHigh.Count); // 2 active + 1 deleted in the fixture
    }

    [Fact]
    public async Task Summary_WithoutIsDeletedFilter_DeletedTasksWouldBeCounted()
    {
        // Proves the fixture can detect a missing filter: the same SQL minus its WHERE clause counts deleted rows.
        await InsertFixtureAsync();
        const string filter = "WHERE [IsDeleted] = 0";
        var unfilteredSql = TaskSummaryService.SummarySql.Replace(filter, string.Empty);
        Assert.NotEqual(TaskSummaryService.SummarySql, unfilteredSql);

        var unfiltered = await RunSqlAsync(unfilteredSql);
        var actual = await GetSummaryAsync();

        Assert.Equal(3, unfiltered[(TaskItemStatus.Todo, TaskPriority.High)]);
        Assert.True(unfiltered.ContainsKey((TaskItemStatus.Done, TaskPriority.Low)));

        Assert.Equal(2, actual.Single(s => s.Status == TaskItemStatus.Todo && s.Priority == TaskPriority.High).Count);
        Assert.DoesNotContain(actual, s => s.Status == TaskItemStatus.Done && s.Priority == TaskPriority.Low);
    }

    [Fact]
    public async Task Summary_JsonShape_UsesStringEnumsAndNumericCount()
    {
        await InsertAsync((TaskItemStatus.InProgress, TaskPriority.Critical, 2, false));

        using var json = JsonDocument.Parse(await _client.GetStringAsync(Url));
        var item = Assert.Single(json.RootElement.EnumerateArray());

        Assert.Equal("InProgress", item.GetProperty("status").GetString());
        Assert.Equal("Critical", item.GetProperty("priority").GetString());
        Assert.Equal(JsonValueKind.Number, item.GetProperty("count").ValueKind);
        Assert.Equal(2, item.GetProperty("count").GetInt32());
        Assert.Equal(["count", "priority", "status"], item.EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public async Task Summary_RouteDoesNotCollideWithGetById()
    {
        var task = await InsertAsync((TaskItemStatus.Todo, TaskPriority.Low, 1, false));

        var summary = await _client.GetAsync(Url);
        var byId = await _client.GetAsync($"/api/tasks/{task}");

        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(await summary.Content.ReadAsStringAsync()).RootElement.ValueKind);
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
    }

    [Fact]
    public async Task Summary_ReflectsDeletesMadeThroughTheApi()
    {
        var id = await InsertAsync((TaskItemStatus.Done, TaskPriority.Medium, 1, false));
        Assert.Single(await GetSummaryAsync());

        await _client.DeleteAsync($"/api/tasks/{id}");

        Assert.Empty(await GetSummaryAsync());
    }

    // ---------- helpers ----------

    /// <summary>
    /// Active: Todo/High x2, Todo/Medium x1, Todo/Low x1, InProgress/Critical x1, Done/Critical x3.
    /// Deleted: Todo/High x1 (must not raise its count), Done/Low x1 (combination that must be absent).
    /// </summary>
    private Task InsertFixtureAsync() => InsertAsync(
        (TaskItemStatus.Done, TaskPriority.Critical, 3, false),
        (TaskItemStatus.Todo, TaskPriority.Low, 1, false),
        (TaskItemStatus.Todo, TaskPriority.High, 2, false),
        (TaskItemStatus.InProgress, TaskPriority.Critical, 1, false),
        (TaskItemStatus.Todo, TaskPriority.Medium, 1, false),
        (TaskItemStatus.Todo, TaskPriority.High, 1, true),
        (TaskItemStatus.Done, TaskPriority.Low, 1, true));

    /// <summary>Inserts the requested rows and returns the id of the last one.</summary>
    private async Task<int> InsertAsync(params (TaskItemStatus Status, TaskPriority Priority, int Count, bool Deleted)[] groups)
    {
        await using var db = fixture.CreateDbContext();
        TaskItem? last = null;
        foreach (var (status, priority, count, deleted) in groups)
        {
            for (var i = 0; i < count; i++)
            {
                last = new TaskItem
                {
                    Title = $"{status}/{priority} #{i + 1}{(deleted ? " (deleted)" : string.Empty)}",
                    Status = status,
                    Priority = priority,
                    IsDeleted = deleted,
                    CreatedDate = Created,
                    ModifiedDate = Created,
                };
                db.Tasks.Add(last);
            }
        }

        await db.SaveChangesAsync();
        return last!.Id;
    }

    private async Task<List<TaskSummaryItem>> GetSummaryAsync() =>
        (await _client.GetFromJsonAsync<List<TaskSummaryItem>>(Url, ApiAssert.Json))!;

    private static async Task<Dictionary<(TaskItemStatus, TaskPriority), int>> RunSqlAsync(string sql)
    {
        await using var connection = new SqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var result = new Dictionary<(TaskItemStatus, TaskPriority), int>();
        while (await reader.ReadAsync())
        {
            result[(Enum.Parse<TaskItemStatus>(reader.GetString(0)), Enum.Parse<TaskPriority>(reader.GetString(1)))] = reader.GetInt32(2);
        }

        return result;
    }
}
