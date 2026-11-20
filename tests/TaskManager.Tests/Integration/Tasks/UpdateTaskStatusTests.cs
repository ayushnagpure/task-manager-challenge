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
public sealed class UpdateTaskStatusTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Created = new(2026, 2, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- success ----------

    [Fact]
    public async Task Patch_ChangesStatus_ReturnsFreshRowVersion_AndKeepsOtherFields()
    {
        var original = await InsertAndGetAsync();

        var response = await PatchAsync(original.Id, new { status = "Done" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!;
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.NotEqual(original.RowVersion, updated.RowVersion);   // fresh rowVersion for the UI's copy
        Assert.True(updated.ModifiedDate > original.ModifiedDate);   // ModifiedDate moves forward
        Assert.Equal(original.CreatedDate, updated.CreatedDate);     // CreatedDate never changes
        Assert.Equal(original.Title, updated.Title);
        Assert.Equal(original.Description, updated.Description);
        Assert.Equal(original.Priority, updated.Priority);
        Assert.Equal(original.AssignedTo, updated.AssignedTo);

        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}", ApiAssert.Json);
        Assert.Equal(TaskItemStatus.Done, fetched!.Status);
        Assert.Equal(updated.RowVersion, fetched.RowVersion);
    }

    [Fact]
    public async Task Patch_TwiceInARow_LastWriteWins()
    {
        var original = await InsertAndGetAsync();

        var first = await PatchAsync(original.Id, new { status = "InProgress" });
        var second = await PatchAsync(original.Id, new { status = "Done" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var fetched = await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{original.Id}", ApiAssert.Json);
        Assert.Equal(TaskItemStatus.Done, fetched!.Status);
    }

    [Fact]
    public async Task Patch_StatusIsCaseInsensitive()
    {
        var original = await InsertAndGetAsync();

        var response = await PatchAsync(original.Id, new { status = "inprogress" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TaskItemStatus.InProgress, (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!.Status);
    }

    [Fact]
    public async Task Patch_SameStatus_IsANoOp()
    {
        var original = await InsertAndGetAsync(); // status Todo

        var response = await PatchAsync(original.Id, new { status = "Todo" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!;
        Assert.Equal(original.RowVersion, result.RowVersion);
        Assert.Equal(original.ModifiedDate, result.ModifiedDate);
    }

    // ---------- interaction with PUT ----------

    [Fact]
    public async Task PutAfterPatch_WithPrePatchRowVersion_Returns409_WithPatchedRowVersion_Returns200()
    {
        var original = await InsertAndGetAsync();
        var patched = (await (await PatchAsync(original.Id, new { status = "Done" })).Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json))!;

        // The UI forgot to refresh its copy after the PATCH.
        var stale = await PutAsync(original.Id, Convert.ToBase64String(original.RowVersion));
        await ApiAssert.ProblemAsync(stale, HttpStatusCode.Conflict);

        // The UI used the rowVersion returned by the PATCH.
        var fresh = await PutAsync(original.Id, Convert.ToBase64String(patched.RowVersion));
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
    }

    // ---------- validation ----------

    [Fact]
    public async Task Patch_InvalidStatus_Returns400ListingAllowedValues()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(await PatchAsync(original.Id, new { status = "Blocked" }), "status");

        Assert.Equal("Status must be one of: Todo, InProgress, Done.", ApiAssert.FirstError(body, "status"));
    }

    [Fact]
    public async Task Patch_MissingStatus_Returns400()
    {
        var original = await InsertAndGetAsync();

        var body = await ApiAssert.ValidationErrorAsync(await PatchAsync(original.Id, new { }), "status");

        Assert.Equal("Status is required.", ApiAssert.FirstError(body, "status"));
    }

    [Fact]
    public async Task Patch_NumericStatus_Returns400()
    {
        var original = await InsertAndGetAsync();

        await ApiAssert.ValidationErrorAsync(await PatchRawAsync(original.Id, """{"status":2}"""), "$.status");
    }

    [Fact]
    public async Task Patch_OnlyChangesStatus_EvenIfOtherFieldsAreSent()
    {
        var original = await InsertAndGetAsync();

        var response = await PatchAsync(original.Id, new { status = "Done", title = "Hijacked", priority = "Critical", isDeleted = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = fixture.CreateDbContext();
        var row = await db.Tasks.IgnoreQueryFilters().SingleAsync(t => t.Id == original.Id);
        Assert.Equal(original.Title, row.Title);
        Assert.Equal(original.Priority, row.Priority);
        Assert.False(row.IsDeleted);
        Assert.Equal(TaskItemStatus.Done, row.Status);
    }

    // ---------- not found ----------

    [Fact]
    public async Task Patch_UnknownId_Returns404()
    {
        var body = await ApiAssert.ProblemAsync(await PatchAsync(999999, new { status = "Done" }), HttpStatusCode.NotFound);

        Assert.Equal("Task with id '999999' was not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Patch_SoftDeletedId_Returns404_AndDoesNotChangeTheRow()
    {
        var deleted = NewTask();
        deleted.IsDeleted = true;
        await using (var db = fixture.CreateDbContext())
        {
            db.Tasks.Add(deleted);
            await db.SaveChangesAsync();
        }

        await ApiAssert.ProblemAsync(await PatchAsync(deleted.Id, new { status = "Done" }), HttpStatusCode.NotFound);

        await using var verify = fixture.CreateDbContext();
        Assert.Equal(TaskItemStatus.Todo, await verify.Tasks.IgnoreQueryFilters().Where(t => t.Id == deleted.Id).Select(t => t.Status).SingleAsync());
    }

    // ---------- helpers ----------

    private static TaskItem NewTask() => new()
    {
        Title = "Status target",
        Description = "Unchanged by PATCH",
        AssignedTo = "Dana",
        Status = TaskItemStatus.Todo,
        Priority = TaskPriority.Medium,
        CreatedDate = Created,
        ModifiedDate = Created,
    };

    private async Task<TaskResponse> InsertAndGetAsync()
    {
        var task = NewTask();
        await using (var db = fixture.CreateDbContext())
        {
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
        }

        return (await _client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{task.Id}", ApiAssert.Json))!;
    }

    private Task<HttpResponseMessage> PatchAsync(int id, object body) =>
        PatchRawAsync(id, JsonSerializer.Serialize(body));

    private Task<HttpResponseMessage> PatchRawAsync(int id, string json) =>
        _client.PatchAsync($"/api/tasks/{id}/status", new StringContent(json, Encoding.UTF8, "application/json"));

    private Task<HttpResponseMessage> PutAsync(int id, string rowVersion) =>
        _client.PutAsync($"/api/tasks/{id}", new StringContent(
            JsonSerializer.Serialize(new { title = "Edited", status = "Done", priority = "Medium", rowVersion }),
            Encoding.UTF8,
            "application/json"));
}
