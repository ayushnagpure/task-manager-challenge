using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Tests.Integration.Database;

namespace TaskManager.Tests.Integration.Tasks;

[Collection(DatabaseCollection.Name)]
public sealed class UpdateTaskTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Created = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- success ----------

    [Fact]
    public async Task Put_Valid_Returns200WithNewValuesAndNewRowVersion()
    {
        var original = await InsertAndGetAsync();
        var before = DateTimeOffset.UtcNow;

        var response = await PutAsync(original.Id, new
        {
            title = "  Updated title  ",
            description = "Updated description",
            status = "Done",
            priority = "Critical",
            assignedTo = "Bob",
            rowVersion = original.RowVersion,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!;
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.Equal(TaskPriority.Critical, updated.Priority);
        Assert.Equal("Bob", updated.AssignedTo);
        Assert.NotEqual(Convert.FromBase64String(original.RowVersion), updated.RowVersion);

        // Dates: CreatedDate never changes; ModifiedDate moves forward to "now".
        Assert.Equal(Created, updated.CreatedDate);
        Assert.True(updated.ModifiedDate > Created);
        Assert.True(updated.ModifiedDate >= before.AddSeconds(-5), $"ModifiedDate {updated.ModifiedDate:O} should be ~now");

        // GET reflects the update.
        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}", ApiAssert.Json);
        Assert.Equal(updated with { RowVersion = [] }, fetched! with { RowVersion = [] });
        Assert.Equal(updated.RowVersion, fetched.RowVersion);
    }

    [Fact]
    public async Task Put_OmittedOptionalFields_AreCleared()
    {
        var original = await InsertAndGetAsync(description: "Will be cleared", assignedTo: "Alice");

        var response = await PutAsync(original.Id, new { title = "Same", status = "Todo", priority = "Low", rowVersion = original.RowVersion });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!;
        Assert.Null(updated.Description);
        Assert.Null(updated.AssignedTo);
    }

    [Fact]
    public async Task Put_OverPostedFields_AreIgnored()
    {
        var original = await InsertAndGetAsync();

        var response = await PutAsync(original.Id, new
        {
            id = 999,
            title = "Over-post",
            status = "Todo",
            priority = "Low",
            rowVersion = original.RowVersion,
            createdDate = "2000-01-01T00:00:00Z",
            modifiedDate = "2000-01-01T00:00:00Z",
            isDeleted = true,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!;
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(Created, updated.CreatedDate);
        Assert.NotEqual(2000, updated.ModifiedDate.Year);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Tasks.IgnoreQueryFilters().Where(t => t.Id == original.Id).Select(t => t.IsDeleted).SingleAsync());
    }

    // ---------- concurrency ----------

    [Fact]
    public async Task Put_StaleRowVersion_Returns409_AndKeepsTheFirstWritersValues()
    {
        var original = await InsertAndGetAsync();

        var first = await PutAsync(original.Id, new { title = "First writer", status = "InProgress", priority = "High", rowVersion = original.RowVersion });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Second writer still holds the original rowVersion.
        var second = await PutAsync(original.Id, new { title = "Second writer", status = "Done", priority = "Low", rowVersion = original.RowVersion });

        var problem = await ApiAssert.ProblemAsync(second, HttpStatusCode.Conflict);
        Assert.Equal("The task was changed by someone else. Reload it and try again.", problem.GetProperty("detail").GetString());

        await using var db = fixture.CreateDbContext();
        var row = await db.Tasks.SingleAsync(t => t.Id == original.Id);
        Assert.Equal("First writer", row.Title);
        Assert.Equal(TaskItemStatus.InProgress, row.Status);
    }

    // ---------- rowVersion validation ----------

    [Fact]
    public async Task Put_MissingRowVersion_Returns400()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "x", status = "Todo", priority = "Low" }), "rowVersion");

        Assert.Equal("RowVersion is required.", ApiAssert.FirstError(body, "rowVersion"));
    }

    [Theory]
    [InlineData("not-base64!!")]
    [InlineData("AAAA")]                     // valid base64, but 3 bytes
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==")] // valid base64, but 16 bytes
    public async Task Put_MalformedRowVersion_Returns400(string rowVersion)
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "x", status = "Todo", priority = "Low", rowVersion }), "rowVersion");

        Assert.Equal("RowVersion must be the base64 value returned by the API.", ApiAssert.FirstError(body, "rowVersion"));
    }

    [Fact]
    public async Task Put_EmptyRowVersion_ReportsOnlyRequired()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "x", status = "Todo", priority = "Low", rowVersion = "" }), "rowVersion");

        Assert.Equal(1, body.GetProperty("errors").GetProperty("rowVersion").GetArrayLength());
        Assert.Equal("RowVersion is required.", ApiAssert.FirstError(body, "rowVersion"));
    }

    // ---------- field validation ----------

    [Fact]
    public async Task Put_InvalidStatus_Returns400ListingAllowedValues()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "x", status = "Blocked", priority = "Low", rowVersion = original.RowVersion }), "status");

        Assert.Equal("Status must be one of: Todo, InProgress, Done.", ApiAssert.FirstError(body, "status"));
    }

    [Fact]
    public async Task Put_InvalidPriority_Returns400ListingAllowedValues()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "x", status = "Todo", priority = "Urgent", rowVersion = original.RowVersion }), "priority");

        Assert.Equal("Priority must be one of: Low, Medium, High, Critical.", ApiAssert.FirstError(body, "priority"));
    }

    [Fact]
    public async Task Put_MissingStatus_Returns400()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "x", priority = "Low", rowVersion = original.RowVersion }), "status");

        Assert.Equal("Status is required.", ApiAssert.FirstError(body, "status"));
    }

    [Fact]
    public async Task Put_BlankTitle_Returns400()
    {
        var original = await InsertAndGetAsync();

        await ApiAssert.ValidationErrorAsync(
            await PutAsync(original.Id, new { title = "  ", status = "Todo", priority = "Low", rowVersion = original.RowVersion }), "title");
    }

    [Fact]
    public async Task Put_InvalidRequest_DoesNotChangeTheRow()
    {
        var original = await InsertAndGetAsync();

        await PutAsync(original.Id, new { title = "", status = "Blocked", priority = "Low", rowVersion = original.RowVersion });

        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}", ApiAssert.Json);
        Assert.Equal(original.Title, fetched!.Title);
        Assert.Equal(Convert.FromBase64String(original.RowVersion), fetched.RowVersion);
    }

    // ---------- not found ----------

    [Fact]
    public async Task Put_UnknownId_Returns404()
    {
        var body = await ApiAssert.ProblemAsync(
            await PutAsync(999999, new { title = "x", status = "Todo", priority = "Low", rowVersion = "AAAAAAAAB9E=" }),
            HttpStatusCode.NotFound);

        Assert.Equal("Task with id '999999' was not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Put_SoftDeletedId_Returns404()
    {
        var deleted = NewTask("Deleted");
        deleted.IsDeleted = true;
        await using (var db = fixture.CreateDbContext())
        {
            db.Tasks.Add(deleted);
            await db.SaveChangesAsync();
        }

        await ApiAssert.ProblemAsync(
            await PutAsync(deleted.Id, new { title = "x", status = "Todo", priority = "Low", rowVersion = Convert.ToBase64String(deleted.RowVersion) }),
            HttpStatusCode.NotFound);

        await using var verify = fixture.CreateDbContext();
        Assert.Equal("Deleted", await verify.Tasks.IgnoreQueryFilters().Where(t => t.Id == deleted.Id).Select(t => t.Title).SingleAsync());
    }

    [Fact]
    public async Task Put_IdZero_Returns404()
    {
        await ApiAssert.ProblemAsync(
            await PutAsync(0, new { title = "x", status = "Todo", priority = "Low", rowVersion = "AAAAAAAAB9E=" }),
            HttpStatusCode.NotFound);
    }

    // ---------- helpers ----------

    /// <summary>Raw view of a task as the UI sees it: rowVersion is the base64 string from GET.</summary>
    private sealed record ClientTask(int Id, string Title, string RowVersion);

    private static TaskItem NewTask(string title, string? description = null, string? assignedTo = null) => new()
    {
        Title = title,
        Description = description,
        AssignedTo = assignedTo,
        Status = TaskItemStatus.Todo,
        Priority = TaskPriority.Medium,
        CreatedDate = Created,
        ModifiedDate = Created,
    };

    private async Task<ClientTask> InsertAndGetAsync(string? description = null, string? assignedTo = null)
    {
        var task = NewTask("Original", description, assignedTo);
        await using (var db = fixture.CreateDbContext())
        {
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }

        // Read it back through the API, exactly as a client would.
        return (await _client.GetFromJsonAsync<ClientTask>($"/api/tasks/{task.Id}", ApiAssert.Json))!;
    }

    private Task<HttpResponseMessage> PutAsync(int id, object body) =>
        _client.PutAsync($"/api/tasks/{id}", new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
}
