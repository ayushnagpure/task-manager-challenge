using TaskManager.Api.Data;
using TaskManager.Api.Data.Configurations;
using TaskManager.Api.Domain;

namespace TaskManager.Tests.Unit;

public sealed class SeedDataTests
{
    private readonly IReadOnlyList<TaskItem> _tasks = SeedData.Tasks;

    [Fact]
    public void Seed_Has15Tasks_WithUniqueIds1To15()
    {
        Assert.Equal(Enumerable.Range(1, 15), _tasks.Select(t => t.Id).Order());
    }

    [Fact]
    public void Seed_ActiveTasks_CoverEveryStatusAndPriorityExactlyOnce()
    {
        var expected = from s in Enum.GetValues<TaskItemStatus>()
                       from p in Enum.GetValues<TaskPriority>()
                       select (s, p);

        var actual = _tasks.Where(t => !t.IsDeleted).Select(t => (t.Status, t.Priority)).ToList();

        Assert.Equal(12, actual.Count);
        Assert.Equal(expected.OrderBy(x => x).ToList(), actual.OrderBy(x => x).ToList());
    }

    [Fact]
    public void Seed_HasExactlyThreeSoftDeletedTasks()
    {
        Assert.Equal(3, _tasks.Count(t => t.IsDeleted));
    }

    [Fact]
    public void Seed_HasVariedAssignees_IncludingUnassigned()
    {
        Assert.True(_tasks.Select(t => t.AssignedTo).OfType<string>().Distinct().Count() >= 4);
        Assert.Contains(_tasks, t => t.AssignedTo is null);
    }

    [Fact]
    public void Seed_Dates_AreUtc_ModifiedNotBeforeCreated_AndCreatedDistinct()
    {
        Assert.All(_tasks, t =>
        {
            Assert.Equal(TimeSpan.Zero, t.CreatedDate.Offset);
            Assert.Equal(TimeSpan.Zero, t.ModifiedDate.Offset);
            Assert.True(t.ModifiedDate >= t.CreatedDate, $"Task {t.Id}: ModifiedDate is before CreatedDate.");
        });
        Assert.Equal(_tasks.Count, _tasks.Select(t => t.CreatedDate).Distinct().Count());
    }

    [Fact]
    public void Seed_TextFields_FitColumnLimits()
    {
        Assert.All(_tasks, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Title));
            Assert.True(t.Title.Length <= TaskItemConfiguration.TitleMaxLength);
            Assert.True((t.Description?.Length ?? 0) <= TaskItemConfiguration.DescriptionMaxLength);
            Assert.True((t.AssignedTo?.Length ?? 0) <= TaskItemConfiguration.AssignedToMaxLength);
        });
    }

    [Fact]
    public void Seed_ReturnsFreshInstances_SoCallersCannotMutateTheSeed()
    {
        SeedData.Tasks[0].Title = "mutated";

        Assert.NotEqual("mutated", SeedData.Tasks[0].Title);
    }
}
