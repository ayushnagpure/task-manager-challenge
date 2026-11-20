using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Contracts;
using TaskManager.Api.Domain;
using TaskManager.Tests.Integration.Database;

namespace TaskManager.Tests.Integration.Tasks;

[Collection(DatabaseCollection.Name)]
public sealed class CreateTaskTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private const string Url = "/api/tasks";
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    public Task InitializeAsync() => fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- success ----------

    [Fact]
    public async Task Create_Valid_Returns201WithLocationAndTask()
    {
        var response = await _client.PostAsJsonAsync(Url, new
        {
            title = "Write release notes",
            description = "For v1.0",
            status = "InProgress",
            priority = "High",
            assignedTo = "Alice",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json);
        Assert.NotNull(task);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Write release notes", task.Title);
        Assert.Equal("For v1.0", task.Description);
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal("Alice", task.AssignedTo);
        Assert.Equal(task.CreatedDate, task.ModifiedDate);
        Assert.NotEmpty(task.RowVersion);
    }

    [Fact]
    public async Task Create_ThenGetLocation_ReturnsSameTask()
    {
        var response = await _client.PostAsJsonAsync(Url, new { title = "Round trip", priority = "Low" });
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json);

        var fetched = await _client.GetFromJsonAsync<TaskResponse>(response.Headers.Location, ApiAssert.Json);

        Assert.NotNull(created);
        Assert.NotNull(fetched);
        Assert.Equal(created with { RowVersion = [] }, fetched with { RowVersion = [] });
        Assert.Equal(created.RowVersion, fetched.RowVersion);
    }

    [Fact]
    public async Task Create_StatusOmitted_DefaultsToTodo_AndEnumNamesAreCaseInsensitive()
    {
        var response = await _client.PostAsJsonAsync(Url, new { title = "Defaults", priority = "high" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json);
        Assert.Equal(TaskItemStatus.Todo, task!.Status);
        Assert.Equal(TaskPriority.High, task.Priority);
    }

    [Fact]
    public async Task Create_TitleOfExactly200Chars_Succeeds()
    {
        var response = await _client.PostAsJsonAsync(Url, new { title = new string('a', 200), priority = "Low" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_OverPostedFields_AreIgnored()
    {
        var response = await _client.PostAsJsonAsync(Url, new
        {
            id = 999,
            title = "Over-post",
            priority = "Low",
            isDeleted = true,
            createdDate = "2000-01-01T00:00:00Z",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(ApiAssert.Json);
        Assert.NotEqual(999, task!.Id);
        Assert.NotEqual(2000, task.CreatedDate.Year);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.Tasks.IgnoreQueryFilters().Where(t => t.Id == task.Id).Select(t => t.IsDeleted).SingleAsync());
    }

    // ---------- validation failures ----------

    [Fact]
    public async Task Create_MissingTitle_Returns400ForTitle()
    {
        var body = await ApiAssert.ValidationErrorAsync(await _client.PostAsJsonAsync(Url, new { priority = "Low" }), "Title");

        Assert.Contains("Title is required.", body.GetProperty("errors").GetProperty("Title")[0].GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankTitle_Returns400ForTitle(string title)
    {
        await ApiAssert.ValidationErrorAsync(await _client.PostAsJsonAsync(Url, new { title, priority = "Low" }), "Title");
    }

    [Fact]
    public async Task Create_TitleOver200Chars_Returns400ForTitle()
    {
        var body = await ApiAssert.ValidationErrorAsync(
            await _client.PostAsJsonAsync(Url, new { title = new string('a', 201), priority = "Low" }), "Title");

        Assert.Equal("Title must be at most 200 characters.", body.GetProperty("errors").GetProperty("Title")[0].GetString());
    }

    [Fact]
    public async Task Create_DescriptionOver2000Chars_Returns400ForDescription()
    {
        await ApiAssert.ValidationErrorAsync(
            await _client.PostAsJsonAsync(Url, new { title = "x", priority = "Low", description = new string('d', 2001) }), "Description");
    }

    [Fact]
    public async Task Create_AssignedToOver100Chars_Returns400ForAssignedTo()
    {
        await ApiAssert.ValidationErrorAsync(
            await _client.PostAsJsonAsync(Url, new { title = "x", priority = "Low", assignedTo = new string('n', 101) }), "AssignedTo");
    }

    [Fact]
    public async Task Create_InvalidStatus_Returns400ListingAllowedValues()
    {
        var body = await ApiAssert.ValidationErrorAsync(
            await _client.PostAsJsonAsync(Url, new { title = "x", status = "Blocked", priority = "Low" }), "Status");

        Assert.Equal("Status must be one of: Todo, InProgress, Done.", body.GetProperty("errors").GetProperty("Status")[0].GetString());
    }

    [Fact]
    public async Task Create_InvalidPriority_Returns400ListingAllowedValues()
    {
        var body = await ApiAssert.ValidationErrorAsync(
            await _client.PostAsJsonAsync(Url, new { title = "x", priority = "Urgent" }), "Priority");

        Assert.Equal("Priority must be one of: Low, Medium, High, Critical.", body.GetProperty("errors").GetProperty("Priority")[0].GetString());
    }

    [Fact]
    public async Task Create_MissingPriority_Returns400ForPriority()
    {
        await ApiAssert.ValidationErrorAsync(await _client.PostAsJsonAsync(Url, new { title = "x" }), "Priority");
    }

    [Fact]
    public async Task Create_NumericPriority_Returns400()
    {
        await ApiAssert.ValidationErrorAsync(await PostRawAsync("""{"title":"x","priority":2}"""), "priority");
    }

    [Fact]
    public async Task Create_SeveralInvalidFields_ReportsAllOfThem()
    {
        var body = await ApiAssert.ValidationErrorAsync(
            await _client.PostAsJsonAsync(Url, new { title = "", priority = "Urgent" }), "Title");

        Assert.True(body.GetProperty("errors").TryGetProperty("Priority", out _));
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400WithoutInternalDetails()
    {
        var response = await PostRawAsync("""{"title":""");
        var raw = await response.Content.ReadAsStringAsync();

        await ApiAssert.ValidationErrorAsync(response, "$");
        Assert.DoesNotContain("System.Text.Json", raw);
        Assert.DoesNotContain("Exception", raw);
        Assert.DoesNotContain("TaskManager.Api", raw);
    }

    [Fact]
    public async Task Create_EmptyBody_Returns400()
    {
        var response = await PostRawAsync(string.Empty);

        await ApiAssert.ProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_NonJsonContentType_Returns415ProblemDetails()
    {
        var response = await _client.PostAsync(Url, new StringContent("title=x", Encoding.UTF8, "text/plain"));

        await ApiAssert.ProblemAsync(response, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task Create_InvalidRequest_DoesNotInsertAnything()
    {
        await _client.PostAsJsonAsync(Url, new { title = "", priority = "Urgent" });

        await using var db = fixture.CreateDbContext();
        Assert.Equal(0, await db.Tasks.IgnoreQueryFilters().CountAsync());
    }

    private Task<HttpResponseMessage> PostRawAsync(string json) =>
        _client.PostAsync(Url, new StringContent(json, Encoding.UTF8, "application/json"));
}
