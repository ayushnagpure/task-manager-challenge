using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Tests.Integration.Database;

namespace TaskManager.Tests.Integration.Tasks;

[Collection(DatabaseCollection.Name)]
public sealed class GetTaskTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Created = new(2026, 2, 1, 8, 0, 0, TimeSpan.Zero);
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_ExistingTask_Returns200WithAllFields()
    {
        var task = await InsertAsync(new TaskItem
        {
            Title = "Existing",
            Description = "Details",
            Status = TaskItemStatus.Done,
            Priority = TaskPriority.Critical,
            AssignedTo = "Dana",
            CreatedDate = Created,
            ModifiedDate = Created.AddDays(1),
        });

        var response = await _client.GetAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json);
        Assert.NotNull(body);
        Assert.Equal(task.Id, body.Id);
        Assert.Equal("Existing", body.Title);
        Assert.Equal("Details", body.Description);
        Assert.Equal(TaskItemStatus.Done, body.Status);
        Assert.Equal(TaskPriority.Critical, body.Priority);
        Assert.Equal("Dana", body.AssignedTo);
        Assert.Equal(Created, body.CreatedDate);
        Assert.Equal(Created.AddDays(1), body.ModifiedDate);
        Assert.Equal(task.RowVersion, body.RowVersion);
    }

    [Fact]
    public async Task Get_ReturnsEnumsAsStrings_AndRowVersionAsBase64()
    {
        var task = await InsertAsync(NewTask("Json shape"));

        var json = await _client.GetStringAsync($"/api/tasks/{task.Id}");

        Assert.Contains("\"status\":\"Todo\"", json);
        Assert.Contains("\"priority\":\"Medium\"", json);
        Assert.Contains($"\"rowVersion\":\"{Convert.ToBase64String(task.RowVersion)}\"", json);
        Assert.DoesNotContain("isDeleted", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404ProblemDetails()
    {
        var body = await ApiAssert.ProblemAsync(await _client.GetAsync("/api/tasks/999999"), HttpStatusCode.NotFound);

        Assert.Equal("Task with id '999999' was not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Get_SoftDeletedId_Returns404_SameAsUnknown()
    {
        var deleted = NewTask("Deleted");
        deleted.IsDeleted = true;
        deleted = await InsertAsync(deleted);

        var body = await ApiAssert.ProblemAsync(await _client.GetAsync($"/api/tasks/{deleted.Id}"), HttpStatusCode.NotFound);

        Assert.Equal("Resource not found", body.GetProperty("title").GetString());
        Assert.Equal($"Task with id '{deleted.Id}' was not found.", body.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("99999999999")] // overflows int
    public async Task Get_InvalidId_Returns404ProblemDetails(string id)
    {
        await ApiAssert.ProblemAsync(await _client.GetAsync($"/api/tasks/{id}"), HttpStatusCode.NotFound);
    }

    private static TaskItem NewTask(string title) => new()
    {
        Title = title,
        Status = TaskItemStatus.Todo,
        Priority = TaskPriority.Medium,
        CreatedDate = Created,
        ModifiedDate = Created,
    };

    private async Task<TaskItem> InsertAsync(TaskItem task)
    {
        await using var db = fixture.CreateDbContext();
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }
}
