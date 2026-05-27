using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Tasks;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.ToTable("tasks");
        b.HasKey(x => x.Id);

        b.Property(x => x.Number).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Description);
        b.Property(x => x.Column).HasColumnType("task_column").HasColumnName("column").IsRequired();
        b.Property(x => x.Priority).HasColumnType("priority_level").IsRequired();
        b.Property(x => x.Position).HasColumnType("numeric(20,10)").IsRequired();
        b.Property(x => x.EstimateHours).HasColumnType("numeric(6,2)");
        b.Property(x => x.DueDate);
        b.Property(x => x.CompletedAt).HasColumnType("timestamptz");
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        b.Property(x => x.DeletedAt).HasColumnType("timestamptz");

        // Generated tsvector column maintained by Postgres
        b.Property<NpgsqlTypes.NpgsqlTsVector>("search_tsv")
            .HasColumnName("search_tsv")
            .HasComputedColumnSql(
                "setweight(to_tsvector('english', coalesce(title, '')), 'A') || "
                    + "setweight(to_tsvector('english', coalesce(description, '')), 'B')",
                stored: true
            );
        b.HasIndex("search_tsv").HasMethod("gin");

        b.HasIndex(x => new { x.ProjectId, x.Number }).IsUnique();
        b.HasIndex(x => new
            {
                x.ProjectId,
                x.Column,
                x.Position,
            })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_tasks_board");
        b.HasIndex(x => x.AssigneeId)
            .HasFilter("deleted_at IS NULL AND column <> 'done'")
            .HasDatabaseName("ix_tasks_assignee_open");
        b.HasIndex(x => new { x.WorkspaceId, x.DueDate }).HasFilter("deleted_at IS NULL");

        b.HasOne<TeamFlow.Domain.Projects.Project>()
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasQueryFilter(x => x.DeletedAt == null);
        b.Ignore(x => x.DomainEvents);

        b.HasMany(x => x.Tags)
            .WithOne()
            .HasForeignKey(t => t.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Watchers)
            .WithOne()
            .HasForeignKey(w => w.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Dependencies)
            .WithOne()
            .HasForeignKey(d => d.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Comments)
            .WithOne()
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Tags).HasField("_tags").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Watchers)
            .HasField("_watchers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Dependencies)
            .HasField("_dependencies")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Comments)
            .HasField("_comments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> b)
    {
        b.ToTable("task_tags");
        b.HasKey(x => new { x.TaskId, x.TagId });
        b.HasOne<TeamFlow.Domain.Workspaces.Tag>()
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TaskWatcherConfiguration : IEntityTypeConfiguration<TaskWatcher>
{
    public void Configure(EntityTypeBuilder<TaskWatcher> b)
    {
        b.ToTable("task_watchers");
        b.HasKey(x => new { x.TaskId, x.UserId });
    }
}

internal sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> b)
    {
        b.ToTable("task_dependencies");
        b.HasKey(x => new { x.TaskId, x.DependsOnId });
        b.ToTable(t =>
            t.HasCheckConstraint("ck_task_dependencies_no_self", "task_id <> depends_on_id")
        );
        b.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(x => x.DependsOnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> b)
    {
        b.ToTable("task_comments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.EditedAt).HasColumnType("timestamptz");
        b.Property(x => x.DeletedAt).HasColumnType("timestamptz");
        b.HasOne<TaskComment>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TaskId, x.CreatedAt });
        b.HasQueryFilter(x => x.DeletedAt == null);
    }
}
