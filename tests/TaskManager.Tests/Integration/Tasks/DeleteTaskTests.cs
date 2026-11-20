using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Tests.Integration.Database;

namespace TaskManager.Tests.Integration.Tasks;

[Collection(DatabaseCollection.Name)]
public sealed class DeleteTaskTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Created = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Delete_Returns204WithNoBody_ThenGetReturns404()
    {
        var task = await InsertAsync(NewTask("To delete"));

        var response = await _client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());

        await ApiAssert.ProblemAsync(await _client.GetAsync($"/api/tasks/{task.Id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_KeepsTheRow_WithIsDeletedTrue_AndUpdatesModifiedDateOnly()
    {
        var task = await InsertAsync(NewTask("Keep my row", TaskItemStatus.InProgress, TaskPriority.Critical, assignedTo: "Alice"));

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/tasks/{task.Id}")).StatusCode);

        await using var db = fixture.CreateDbContext();
        var row = await db.Tasks.IgnoreQueryFilters().SingleAsync(t => t.Id == task.Id);
        Assert.True(row.IsDeleted);
        Assert.True(row.ModifiedDate > Created, "ModifiedDate should record the deletion time");
        Assert.Equal(Created, row.CreatedDate);
        Assert.Equal("Keep my row", row.Title);
        Assert.Equal(TaskItemStatus.InProgress, row.Status);
        Assert.Equal(TaskPriority.Critical, row.Priority);
        Assert.Equal("Alice", row.AssignedTo);
        Assert.NotEqual(task.RowVersion, row.RowVersion);
    }

    [Fact]
    public async Task Delete_RemovesTaskFromList_AndFromAMatchingFilteredList()
    {
        var doomed = await InsertAsync(NewTask("Doomed", TaskItemStatus.Todo, TaskPriority.High));
        await InsertAsync(NewTask("Survivor", TaskItemStatus.Todo, TaskPriority.High));

        await _client.DeleteAsync($"/api/tasks/{doomed.Id}");

        var all = await _client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks", ApiAssert.Json);
        var filtered = await _client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks?status=Todo&priority=High", ApiAssert.Json);
        Assert.Equal(["Survivor"], all!.Select(t => t.Title));
        Assert.Equal(["Survivor"], filtered!.Select(t => t.Title));
    }

    [Fact]
    public async Task Delete_Twice_SecondReturns404()
    {
        var task = await InsertAsync(NewTask("Once only"));

        var first = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        var second = await _client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        var body = await ApiAssert.ProblemAsync(second, HttpStatusCode.NotFound);
        Assert.Equal($"Task with id '{task.Id}' was not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var body = await ApiAssert.ProblemAsync(await _client.DeleteAsync("/api/tasks/999999"), HttpStatusCode.NotFound);

        Assert.Equal("Task with id '999999' was not found.", body.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public async Task Delete_InvalidId_Returns404(string id)
    {
        await ApiAssert.ProblemAsync(await _client.DeleteAsync($"/api/tasks/{id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletedTask_CannotBeUpdatedOrPatched()
    {
        var task = await InsertAsync(NewTask("Deleted then edited"));
        await _client.DeleteAsync($"/api/tasks/{task.Id}");

        var put = await _client.PutAsync($"/api/tasks/{task.Id}", Json(new
        {
            title = "Revived?",
            status = "Todo",
            priority = "Low",
            rowVersion = Convert.ToBase64String(task.RowVersion),
        }));
        var patch = await _client.PatchAsync($"/api/tasks/{task.Id}/status", Json(new { status = "Done" }));

        await ApiAssert.ProblemAsync(put, HttpStatusCode.NotFound);
        await ApiAssert.ProblemAsync(patch, HttpStatusCode.NotFound);

        await using var db = fixture.CreateDbContext();
        var row = await db.Tasks.IgnoreQueryFilters().SingleAsync(t => t.Id == task.Id);
        Assert.True(row.IsDeleted);
        Assert.Equal("Deleted then edited", row.Title);
    }

    private static TaskItem NewTask(
        string title,
        TaskItemStatus status = TaskItemStatus.Todo,
        TaskPriority priority = TaskPriority.Medium,
        string? assignedTo = null) => new()
    {
        Title = title,
        Status = status,
        Priority = priority,
        AssignedTo = assignedTo,
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

    private static StringContent Json(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
}
