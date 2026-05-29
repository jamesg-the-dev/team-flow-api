using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Activity;
using TeamFlow.Domain.Attachments;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Identity;
using TeamFlow.Domain.Notifications;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Tasks;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Infrastructure.Persistence;

public sealed class TeamFlowDbContext : DbContext
{
    public TeamFlowDbContext(DbContextOptions<TeamFlowDbContext> options)
        : base(options) { }

    // Identity
    public DbSet<Profile> Profiles => Set<Profile>();

    // Workspaces
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<WorkspaceInvite> WorkspaceInvites => Set<WorkspaceInvite>();
    public DbSet<Tag> Tags => Set<Tag>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTag> ProjectTags => Set<ProjectTag>();

    // Tasks
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskTag> TaskTags => Set<TaskTag>();
    public DbSet<TaskWatcher> TaskWatchers => Set<TaskWatcher>();
    public DbSet<TaskDependency> TaskDependencies => Set<TaskDependency>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    // Discussions
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelMember> ChannelMembers => Set<ChannelMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<MessageMention> MessageMentions => Set<MessageMention>();

    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // All IEntityTypeConfiguration<> in this assembly are applied here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamFlowDbContext).Assembly);

        // Map domain enums to Postgres enum types (one-time DDL handled in migrations).
        modelBuilder.HasPostgresEnum<Domain.Enums.WorkspaceRole>("public", "workspace_role");
        modelBuilder.HasPostgresEnum<Domain.Enums.ProjectStatus>("public", "project_status");
        modelBuilder.HasPostgresEnum<Domain.Enums.ProjectMemberRole>(
            "public",
            "project_member_role"
        );
        modelBuilder.HasPostgresEnum<Domain.Enums.PriorityLevel>("public", "priority_level");
        modelBuilder.HasPostgresEnum<Domain.Enums.TaskColumn>("public", "task_column");
        modelBuilder.HasPostgresEnum<Domain.Enums.ChannelType>("public", "channel_type");
        modelBuilder.HasPostgresEnum<Domain.Enums.NotificationKind>("public", "notification_kind");
        modelBuilder.HasPostgresEnum<Domain.Enums.DeliveryChannel>("public", "delivery_channel");
        modelBuilder.HasPostgresEnum<Domain.Enums.AttachmentOwner>("public", "attachment_owner");

        base.OnModelCreating(modelBuilder);
    }
}
