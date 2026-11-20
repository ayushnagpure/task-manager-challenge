using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Domain;

namespace TaskManager.Api.Data;

/// <summary>EF Core context for the task manager database.</summary>
public class TaskDbContext : DbContext
{
    /// <summary>Creates the context.</summary>
    public TaskDbContext(DbContextOptions<TaskDbContext> options)
        : base(options)
    {
    }

    /// <summary>Tasks. Soft-deleted rows are excluded unless <c>IgnoreQueryFilters()</c> is used.</summary>
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskDbContext).Assembly);
}
