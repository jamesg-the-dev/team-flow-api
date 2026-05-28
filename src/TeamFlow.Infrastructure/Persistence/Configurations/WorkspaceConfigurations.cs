using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

internal sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> b)
    {
        b.ToTable("workspaces");
        b.HasKey(x => x.Id);
        b.Property(x => x.Slug).HasColumnType("citext").IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Plan).HasMaxLength(50).IsRequired();
        b.Property(x => x.LogoUrl).HasMaxLength(2000);
        b.Property(x => x.OwnerId).IsRequired();

        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.UpdatedAt).HasColumnType("timestamptz");
        b.Property(x => x.DeletedAt).HasColumnType("timestamptz");

        b.HasIndex(x => x.OwnerId);
        b.HasQueryFilter(x => x.DeletedAt == null);

        b.Ignore(x => x.DomainEvents);

        // child collections
        b.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Invites)
            .WithOne()
            .HasForeignKey(i => i.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Tags)
            .WithOne()
            .HasForeignKey(t => t.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Navigation(x => x.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Invites)
            .HasField("_invites")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Tags).HasField("_tags").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> b)
    {
        b.ToTable("workspace_members");
        b.HasKey(x => new { x.WorkspaceId, x.UserId });
        b.Property(x => x.Role).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.JoinedAt).HasColumnType("timestamptz");
        b.HasIndex(x => x.UserId);
    }
}

internal sealed class WorkspaceInviteConfiguration : IEntityTypeConfiguration<WorkspaceInvite>
{
    public void Configure(EntityTypeBuilder<WorkspaceInvite> b)
    {
        b.ToTable("workspace_invites");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasColumnType("citext").IsRequired();
        b.Property(x => x.Role).IsRequired();
        b.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.WorkspaceId, x.Email }).IsUnique();
        b.Property(x => x.ExpiresAt).HasColumnType("timestamptz");
        b.Property(x => x.AcceptedAt).HasColumnType("timestamptz");
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasColumnType("citext").IsRequired();
        b.HasIndex(x => new { x.WorkspaceId, x.Name }).IsUnique();
        b.Property(x => x.ColorHex).HasMaxLength(7).IsRequired();
    }
}
