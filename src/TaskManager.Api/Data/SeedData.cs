using TaskManager.Api.Domain;

namespace TaskManager.Api.Data;

/// <summary>
/// Sample tasks inserted by the <c>SeedTasks</c> migration through <c>HasData</c>.
/// Ids and dates are fixed literals (never <c>UtcNow</c>) so later migrations don't re-emit the rows.
/// Ids 1-12 are active and cover every status x priority combination once; ids 13-15 are soft-deleted.
/// </summary>
public static class SeedData
{
    /// <summary>Returns fresh instances of the seed tasks on every call.</summary>
    public static IReadOnlyList<TaskItem> Tasks =>
    [
        // Active: Todo
        Item(1, "Archive old project files", "Move 2025 project folders to cold storage.",
            TaskItemStatus.Todo, TaskPriority.Low, "Bob", created: Utc(2026, 1, 2, 9), modified: Utc(2026, 1, 2, 9)),
        Item(2, "Update team wiki", "Refresh the onboarding and on-call pages.",
            TaskItemStatus.Todo, TaskPriority.Medium, "Chen", created: Utc(2026, 1, 3, 10), modified: Utc(2026, 1, 4, 11)),
        Item(3, "Prepare sprint demo", "Slides and a demo script for Friday's review.",
            TaskItemStatus.Todo, TaskPriority.High, "Alice", created: Utc(2026, 1, 4, 9), modified: Utc(2026, 1, 4, 9)),
        Item(4, "Fix login outage", "Users intermittently get 500s on sign-in.",
            TaskItemStatus.Todo, TaskPriority.Critical, "Dana", created: Utc(2026, 1, 5, 8), modified: Utc(2026, 1, 5, 8)),

        // Active: InProgress
        Item(5, "Tidy CSS variables", null,
            TaskItemStatus.InProgress, TaskPriority.Low, null, created: Utc(2026, 1, 6, 14), modified: Utc(2026, 1, 7, 9)),
        Item(6, "Write API integration tests", "Cover filtering, validation and error responses.",
            TaskItemStatus.InProgress, TaskPriority.Medium, "Alice", created: Utc(2026, 1, 7, 10), modified: Utc(2026, 1, 9, 16)),
        Item(7, "Migrate CI to new runners", "Switch the build pipeline to the new runner pool.",
            TaskItemStatus.InProgress, TaskPriority.High, "Bob", created: Utc(2026, 1, 8, 11), modified: Utc(2026, 1, 10, 12)),
        Item(8, "Patch security vulnerability", "Upgrade the affected dependency and redeploy.",
            TaskItemStatus.InProgress, TaskPriority.Critical, "Chen", created: Utc(2026, 1, 9, 7), modified: Utc(2026, 1, 9, 15)),

        // Active: Done
        Item(9, "Rename repository labels", "Align issue labels with the new triage process.",
            TaskItemStatus.Done, TaskPriority.Low, "Dana", created: Utc(2026, 1, 10, 9), modified: Utc(2026, 1, 11, 10)),
        Item(10, "Draft onboarding guide", null,
            TaskItemStatus.Done, TaskPriority.Medium, null, created: Utc(2026, 1, 11, 13), modified: Utc(2026, 1, 13, 17)),
        Item(11, "Set up database backups", "Nightly full backups with 30-day retention.",
            TaskItemStatus.Done, TaskPriority.High, "Alice", created: Utc(2026, 1, 12, 8), modified: Utc(2026, 1, 14, 9)),
        Item(12, "Restore production database", "Recover from the corrupted index incident.",
            TaskItemStatus.Done, TaskPriority.Critical, "Eve", created: Utc(2026, 1, 13, 6), modified: Utc(2026, 1, 13, 18)),

        // Soft-deleted
        Item(13, "Duplicate of \"Update team wiki\"", "Created by mistake.",
            TaskItemStatus.Todo, TaskPriority.Medium, "Chen", created: Utc(2026, 1, 14, 9), modified: Utc(2026, 1, 14, 10), isDeleted: true),
        Item(14, "Evaluate vendor X", "Cancelled after the budget review.",
            TaskItemStatus.InProgress, TaskPriority.High, "Eve", created: Utc(2026, 1, 15, 11), modified: Utc(2026, 1, 16, 9), isDeleted: true),
        Item(15, "Old test task", null,
            TaskItemStatus.Done, TaskPriority.Low, null, created: Utc(2026, 1, 16, 12), modified: Utc(2026, 1, 16, 12), isDeleted: true),
    ];

    private static DateTimeOffset Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);

    private static TaskItem Item(
        int id,
        string title,
        string? description,
        TaskItemStatus status,
        TaskPriority priority,
        string? assignedTo,
        DateTimeOffset created,
        DateTimeOffset modified,
        bool isDeleted = false) =>
        new()
        {
            Id = id,
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            AssignedTo = assignedTo,
            CreatedDate = created,
            ModifiedDate = modified,
            IsDeleted = isDeleted,
        };
}
