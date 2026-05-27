using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TeamFlow.Domain.Discussions;

namespace TeamFlow.Infrastructure.Persistence.Configurations;

internal sealed class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> b)
    {
        b.ToTable("channels");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasColumnType("citext").HasMaxLength(80).IsRequired();
        b.Property(x => x.Topic).HasMaxLength(500);
        b.Property(x => x.Type).HasColumnType("channel_type").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.ArchivedAt).HasColumnType("timestamptz");
        b.HasIndex(x => new { x.WorkspaceId, x.Name }).IsUnique();
        b.Ignore(x => x.DomainEvents);

        b.HasMany(x => x.Members)
            .WithOne()
            .HasForeignKey(m => m.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ChannelMemberConfiguration : IEntityTypeConfiguration<ChannelMember>
{
    public void Configure(EntityTypeBuilder<ChannelMember> b)
    {
        b.ToTable("channel_members");
        b.HasKey(x => new { x.ChannelId, x.UserId });
        b.Property(x => x.JoinedAt).HasColumnType("timestamptz");
        b.Property(x => x.LastReadAt).HasColumnType("timestamptz");
        b.HasIndex(x => x.UserId);
    }
}

internal sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
        b.Property(x => x.EditedAt).HasColumnType("timestamptz");
        b.Property(x => x.DeletedAt).HasColumnType("timestamptz");

        b.Property<NpgsqlTypes.NpgsqlTsVector>("search_tsv")
            .HasColumnName("search_tsv")
            .HasComputedColumnSql("to_tsvector('english', coalesce(body, ''))", stored: true);
        b.HasIndex("search_tsv").HasMethod("gin");

        b.HasIndex(x => new { x.ChannelId, x.CreatedAt })
            .IsDescending(false, true)
            .HasFilter("deleted_at IS NULL AND parent_id IS NULL");
        b.HasIndex(x => new { x.ParentId, x.CreatedAt }).HasFilter("parent_id IS NOT NULL");

        b.HasOne<Message>()
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasQueryFilter(x => x.DeletedAt == null);
        b.Ignore(x => x.DomainEvents);

        b.HasMany(x => x.Reactions)
            .WithOne()
            .HasForeignKey(r => r.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Mentions)
            .WithOne()
            .HasForeignKey(m => m.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Reactions)
            .HasField("_reactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Mentions)
            .HasField("_mentions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> b)
    {
        b.ToTable("message_reactions");
        b.HasKey(x => new
        {
            x.MessageId,
            x.UserId,
            x.Emoji,
        });
        b.Property(x => x.Emoji).HasMaxLength(32);
        b.Property(x => x.CreatedAt).HasColumnType("timestamptz");
    }
}

internal sealed class MessageMentionConfiguration : IEntityTypeConfiguration<MessageMention>
{
    public void Configure(EntityTypeBuilder<MessageMention> b)
    {
        b.ToTable("message_mentions");
        b.HasKey(x => new { x.MessageId, x.UserId });
    }
}
