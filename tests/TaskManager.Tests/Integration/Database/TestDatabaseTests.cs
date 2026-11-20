namespace TaskManager.Tests.Integration.Database;

public sealed class TestDatabaseTests
{
    [Theory]
    [InlineData(@"Server=localhost\SQLEXPRESS;Database=TaskManagerDb;Trusted_Connection=True")]
    [InlineData(@"Server=localhost\SQLEXPRESS;Database=TaskManagerDb_Tests_Backup;Trusted_Connection=True")]
    [InlineData(@"Server=localhost\SQLEXPRESS;Trusted_Connection=True")]
    public void EnsureTestDatabase_NonTestDatabase_Throws(string connectionString)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => TestDatabase.EnsureTestDatabase(connectionString));

        Assert.Contains("must end with '_Tests'", exception.Message);
    }

    [Fact]
    public void EnsureTestDatabase_TestDatabase_ReturnsConnectionString()
    {
        const string connectionString = @"Server=localhost\SQLEXPRESS;Database=TaskManagerDb_Tests;Trusted_Connection=True";

        Assert.Equal(connectionString, TestDatabase.EnsureTestDatabase(connectionString));
    }
}
