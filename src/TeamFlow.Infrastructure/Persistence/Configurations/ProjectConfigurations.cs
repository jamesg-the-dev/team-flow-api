using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("projects");
        b.HasKey(x => x.Id);

        b.Property(x => x.Key).HasMaxLength(10).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description);
        b.Property(x => x.Status).HasColumnType("project_status").IsRequired();
        b.Property(x => x.Priority).HasColumnType("priority_level").IsRequired();
        b.Property(x => x.StartDate);
        b.Property(x => x.DueDate);
        b.Property(x => x.ColorHex).HasMaxLength(7);
        b.Property(x => x.NextTaskNumber).IsRequired().HasDefaultValue(1);

        // Money value object → two columns
        b.OwnsOne(
            x => x.Budget,
            mb =>
            {
                mb.Property(m => m.AmountCents).HasColumnName("budget_cents");
                mb.Property(m => m.Currency).HasColumnName("budget_currency").HasMaxLength(3);
            }
        );

        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        b.Property(x => x.DeletedAt).HasColumnType("timestamptz");

        b.HasIndex(x => new { x.WorkspaceId, x.Key }).IsUnique();
        b.HasIndex(x => new { x.WorkspaceId, x.Status });
        b.HasIndex(x => new { x.WorkspaceId, x.DueDate });

        b.HasQueryFilter(x => x.DeletedAt == null);
        b.Ignore(x => x.DomainEvents);

        b.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Tags)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Tags).HasField("_tags").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> b)
    {
        b.ToTable("project_members");
        b.HasKey(x => new { x.ProjectId, x.UserId });
        b.Property(x => x.Role).HasColumnType("project_member_role").IsRequired();
        b.Property(x => x.AddedAt).HasColumnType("timestamptz");
        b.HasIndex(x => x.UserId);
    }
}

internal sealed class ProjectTagConfiguration : IEntityTypeConfiguration<ProjectTag>
{
    public void Configure(EntityTypeBuilder<ProjectTag> b)
    {
        b.ToTable("project_tags");
        b.HasKey(x => new { x.ProjectId, x.TagId });
        b.HasOne<TeamFlow.Domain.Workspaces.Tag>()
            .WithMany()
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
