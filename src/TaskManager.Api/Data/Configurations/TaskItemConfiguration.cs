using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Api.Domain;

namespace TaskManager.Api.Data.Configurations;

/// <summary>Table mapping for <see cref="TaskItem"/>.</summary>
public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    /// <summary>Maximum length of <see cref="TaskItem.Title"/>.</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Maximum length of <see cref="TaskItem.Description"/>.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>Maximum length of <see cref="TaskItem.AssignedTo"/>.</summary>
    public const int AssignedToMaxLength = 100;

    private const int EnumMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks", table =>
        {
            // Guarantees only known enum names are stored, even for rows written by raw SQL.
            table.HasCheckConstraint("CK_Tasks_Status", InList(nameof(TaskItem.Status), Enum.GetNames<TaskItemStatus>()));
            table.HasCheckConstraint("CK_Tasks_Priority", InList(nameof(TaskItem.Priority), Enum.GetNames<TaskPriority>()));
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(TitleMaxLength);
        builder.Property(t => t.Description).HasMaxLength(DescriptionMaxLength);
        builder.Property(t => t.AssignedTo).HasMaxLength(AssignedToMaxLength);

        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(EnumMaxLength).IsRequired();
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(EnumMaxLength).IsRequired();

        builder.Property(t => t.CreatedDate).IsRequired();
        builder.Property(t => t.ModifiedDate).IsRequired();
        builder.Property(t => t.IsDeleted).IsRequired();
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => new { t.IsDeleted, t.Status, t.Priority })
            .HasDatabaseName("IX_Tasks_IsDeleted_Status_Priority");
    }

    // Values are compile-time enum names, never user input.
    private static string InList(string column, IEnumerable<string> values) =>
        $"[{column}] IN ({string.Join(", ", values.Select(v => $"'{v}'"))})";
}
