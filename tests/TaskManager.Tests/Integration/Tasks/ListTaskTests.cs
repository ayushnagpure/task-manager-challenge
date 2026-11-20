using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Tests.Integration.Database;

namespace TaskManager.Tests.Integration.Tasks;

[Collection(DatabaseCollection.Name)]
public sealed class ListTaskTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private const string Url = "/api/tasks";
    private static readonly DateTimeOffset Day1 = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- filtering ----------

    [Fact]
    public async Task List_NoFilters_ReturnsAllActiveTasks()
    {
        await InsertAsync(
            NewTask("A", TaskItemStatus.Todo, TaskPriority.Low),
            NewTask("B", TaskItemStatus.Done, TaskPriority.High),
            NewTask("C", TaskItemStatus.InProgress, TaskPriority.Medium));

        var titles = await ListTitlesAsync(Url);

        Assert.Equal(3, titles.Count);
    }

    [Fact]
    public async Task List_ByStatus_ReturnsOnlyThatStatus()
    {
        await InsertAsync(
            NewTask("Todo 1", TaskItemStatus.Todo, TaskPriority.Low),
            NewTask("Done 1", TaskItemStatus.Done, TaskPriority.Low),
            NewTask("Done 2", TaskItemStatus.Done, TaskPriority.High));

        var tasks = await ListAsync($"{Url}?status=Done");

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(TaskItemStatus.Done, t.Status));
    }

    [Fact]
    public async Task List_ByPriority_ReturnsOnlyThatPriority()
    {
        await InsertAsync(
            NewTask("Crit 1", TaskItemStatus.Todo, TaskPriority.Critical),
            NewTask("Crit 2", TaskItemStatus.Done, TaskPriority.Critical),
            NewTask("Low 1", TaskItemStatus.Todo, TaskPriority.Low));

        var tasks = await ListAsync($"{Url}?priority=Critical");

        Assert.Equal(2, tasks.Count);
        Assert.All(tasks, t => Assert.Equal(TaskPriority.Critical, t.Priority));
    }

    [Fact]
    public async Task List_ByStatusAndPriority_ReturnsIntersection()
    {
        await InsertAsync(
            NewTask("Match", TaskItemStatus.Todo, TaskPriority.High),
            NewTask("Wrong status", TaskItemStatus.Done, TaskPriority.High),
            NewTask("Wrong priority", TaskItemStatus.Todo, TaskPriority.Low));

        var titles = await ListTitlesAsync($"{Url}?status=Todo&priority=High");

        Assert.Equal(["Match"], titles);
    }

    [Fact]
    public async Task List_FilterValues_AreCaseInsensitive()
    {
        await InsertAsync(NewTask("Match", TaskItemStatus.InProgress, TaskPriority.Critical));

        var titles = await ListTitlesAsync($"{Url}?status=inprogress&priority=CRITICAL");

        Assert.Equal(["Match"], titles);
    }

    [Fact]
    public async Task List_NoMatch_Returns200WithEmptyArray()
    {
        await InsertAsync(NewTask("Only", TaskItemStatus.Todo, TaskPriority.Low));

        var response = await _client.GetAsync($"{Url}?status=Done");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_EmptyDatabase_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_SoftDeletedTasks_AreExcluded_EvenWhenTheyMatchTheFilter()
    {
        var deleted = NewTask("Deleted match", TaskItemStatus.Todo, TaskPriority.High);
        deleted.IsDeleted = true;
        await InsertAsync(deleted, NewTask("Active match", TaskItemStatus.Todo, TaskPriority.High));

        Assert.Equal(["Active match"], await ListTitlesAsync($"{Url}?status=Todo&priority=High"));
        Assert.Equal(["Active match"], await ListTitlesAsync(Url));
    }

    [Fact]
    public async Task List_EmptyFilterValue_IsTreatedAsNoFilter()
    {
        await InsertAsync(NewTask("A", TaskItemStatus.Todo, TaskPriority.Low), NewTask("B", TaskItemStatus.Done, TaskPriority.Low));

        Assert.Equal(2, (await ListTitlesAsync($"{Url}?status=&priority=")).Count);
    }

    // ---------- sorting ----------

    [Fact]
    public async Task List_SortsByPriorityRank_NotAlphabetically()
    {
        // Inserted in an order that is neither rank nor alphabetical. Alphabetical would be Critical, High, Low, Medium.
        await InsertAsync(
            NewTask("Low", TaskItemStatus.Todo, TaskPriority.Low),
            NewTask("Critical", TaskItemStatus.Todo, TaskPriority.Critical),
            NewTask("Medium", TaskItemStatus.Todo, TaskPriority.Medium),
            NewTask("High", TaskItemStatus.Todo, TaskPriority.High));

        Assert.Equal(["Critical", "High", "Medium", "Low"], await ListTitlesAsync(Url));
    }

    [Fact]
    public async Task List_WithinSamePriority_SortsNewestCreatedDateFirst()
    {
        await InsertAsync(
            NewTask("High old", TaskItemStatus.Todo, TaskPriority.High, created: Day1),
            NewTask("High newest", TaskItemStatus.Todo, TaskPriority.High, created: Day1.AddDays(2)),
            NewTask("High middle", TaskItemStatus.Todo, TaskPriority.High, created: Day1.AddDays(1)),
            NewTask("Critical oldest", TaskItemStatus.Todo, TaskPriority.Critical, created: Day1.AddDays(-5)));

        Assert.Equal(["Critical oldest", "High newest", "High middle", "High old"], await ListTitlesAsync(Url));
    }

    // ---------- invalid filters ----------

    [Fact]
    public async Task List_InvalidStatus_Returns400ListingAllowedValues()
    {
        var body = await ApiAssert.ValidationErrorAsync(await _client.GetAsync($"{Url}?status=Blocked"), "status");

        Assert.Equal("Status must be one of: Todo, InProgress, Done.", ApiAssert.FirstError(body, "status"));
    }

    [Fact]
    public async Task List_InvalidPriority_Returns400ListingAllowedValues()
    {
        var body = await ApiAssert.ValidationErrorAsync(await _client.GetAsync($"{Url}?priority=Urgent"), "priority");

        Assert.Equal("Priority must be one of: Low, Medium, High, Critical.", ApiAssert.FirstError(body, "priority"));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("-1")]
    [InlineData("Low,High")]
    public async Task List_NumericOrCombinedPriority_Returns400(string value)
    {
        await ApiAssert.ValidationErrorAsync(await _client.GetAsync($"{Url}?priority={Uri.EscapeDataString(value)}"), "priority");
    }

    [Fact]
    public async Task List_BothFiltersInvalid_ReportsBoth()
    {
        var body = await ApiAssert.ValidationErrorAsync(await _client.GetAsync($"{Url}?status=Blocked&priority=Urgent"), "status");

        Assert.Equal(["priority", "status"], ApiAssert.ErrorKeys(body));
    }

    // ---------- helpers ----------

    private static TaskItem NewTask(string title, TaskItemStatus status, TaskPriority priority, DateTimeOffset? created = null) => new()
    {
        Title = title,
        Status = status,
        Priority = priority,
        CreatedDate = created ?? Day1,
        ModifiedDate = created ?? Day1,
    };

    private async Task InsertAsync(params TaskItem[] tasks)
    {
        await using var db = fixture.CreateDbContext();
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
    }

    private async Task<List<TaskResponse>> ListAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<TaskResponse>>(ApiAssert.Json))!;
    }

    private async Task<List<string>> ListTitlesAsync(string url) =>
        (await ListAsync(url)).Select(t => t.Title).ToList();
}
