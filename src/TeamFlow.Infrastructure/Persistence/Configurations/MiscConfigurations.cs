using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Activity;
using TeamFlow.Domain.Attachments;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.ToTable("attachments");
        b.HasKey(x => x.Id);
        b.Property(x => x.OwnerKind).HasColumnType("attachment_owner").IsRequired();
        b.Property(x => x.FileName).HasMaxLength(500).IsRequired();
        b.Property(x => x.MimeType).HasMaxLength(120).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.HasIndex(x => new { x.OwnerKind, x.OwnerId });
        b.Ignore(x => x.DomainEvents);
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Kind).HasColumnType("notification_kind").IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Body);
        b.Property(x => x.TargetKind).HasMaxLength(50);
        b.Property(x => x.Url).HasMaxLength(2000);
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.ReadAt).HasColumnType("timestamptz");
        b.HasIndex(x => new { x.RecipientId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => x.RecipientId)
            .HasFilter("read_at IS NULL")
            .HasDatabaseName("ix_notifications_unread");
        b.Ignore(x => x.DomainEvents);
    }
}

internal sealed class NotificationPreferenceConfiguration
    : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        b.ToTable("notification_preferences");
        b.HasKey(x => new
        {
            x.UserId,
            x.WorkspaceId,
            x.Kind,
            x.Channel,
        });
        b.Property(x => x.Kind).HasColumnType("notification_kind");
        b.Property(x => x.Channel).HasColumnType("delivery_channel");
    }
}

internal sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> b)
    {
        b.ToTable("activity_events");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityAlwaysColumn();
        b.Property(x => x.Verb).HasMaxLength(100).IsRequired();
        b.Property(x => x.TargetKind).HasMaxLength(50).IsRequired();
        b.Property(x => x.Metadata).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.HasIndex(x => new { x.WorkspaceId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.ProjectId, x.CreatedAt }).IsDescending(false, true);
        b.HasIndex(x => new { x.ActorId, x.CreatedAt }).IsDescending(false, true);
    }
}
