using System.Net;

namespace TaskManager.Tests.Integration;

public sealed class HealthEndpointTests(TaskManagerApiFactory factory) : IClassFixture<TaskManagerApiFactory>
{
    [Fact]
    public async Task Get_Health_Returns200Healthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
