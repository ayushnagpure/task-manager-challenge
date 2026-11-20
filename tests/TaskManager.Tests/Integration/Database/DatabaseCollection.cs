namespace TaskManager.Tests.Integration.Database;

/// <summary>
/// All tests that touch SQL Server join this collection: they share one <see cref="DatabaseFixture"/>
/// and run serially, so resets never interfere with another test's data.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
