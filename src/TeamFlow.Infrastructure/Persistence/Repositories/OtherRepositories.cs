using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Activity;
using TeamFlow.Domain.Attachments;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Notifications;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Infrastructure.Persistence.Repositories;

internal sealed class ChannelRepository : IChannelRepository
{
    private readonly TeamFlowDbContext _ctx;

    public ChannelRepository(TeamFlowDbContext ctx, IUnitOfWork uow)
    {
        _ctx = ctx;
        UnitOfWork = uow;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<Channel?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Channels.Include(c => c.Members).FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(
        Guid workspaceId,
        string name,
        CancellationToken ct = default
    ) => _ctx.Channels.AnyAsync(c => c.WorkspaceId == workspaceId && c.Name == name, ct);

    public Task<bool> IsMemberAsync(
        Guid channelId,
        Guid userId,
        CancellationToken ct = default
    ) =>
        _ctx.ChannelMembers.AsNoTracking()
            .AnyAsync(m => m.ChannelId == channelId && m.UserId == userId, ct);

    public Task<Channel?> FindDirectChannelAsync(
        Guid workspaceId,
        Guid userA,
        Guid userB,
        CancellationToken ct = default
    ) =>
        _ctx
            .Channels.Include(c => c.Members)
            .Where(c =>
                c.WorkspaceId == workspaceId
                && c.Type == TeamFlow.Domain.Enums.ChannelType.Direct
                && c.Members.Count == 2
                && c.Members.Any(m => m.UserId == userA)
                && c.Members.Any(m => m.UserId == userB)
            )
            .FirstOrDefaultAsync(ct);

    public void Add(Channel channel) => _ctx.Channels.Add(channel);

    public void Remove(Channel channel) => _ctx.Channels.Remove(channel);
}

internal sealed class MessageRepository : IMessageRepository
{
    private readonly TeamFlowDbContext _ctx;

    public MessageRepository(TeamFlowDbContext ctx, IUnitOfWork uow)
    {
        _ctx = ctx;
        UnitOfWork = uow;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx
            .Messages.Include(m => m.Reactions)
            .Include(m => m.Mentions)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Message>> GetChannelTimelineAsync(
        Guid channelId,
        DateTimeOffset? before,
        int take,
        CancellationToken ct = default
    ) =>
        await _ctx
            .Messages.Where(m =>
                m.ChannelId == channelId
                && m.ParentId == null
                && (before == null || m.CreatedAt < before)
            )
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Message>> GetThreadAsync(
        Guid parentId,
        CancellationToken ct = default
    ) =>
        await _ctx
            .Messages.Where(m => m.ParentId == parentId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public void Add(Message message) => _ctx.Messages.Add(message);

    public void Remove(Message message) => _ctx.Messages.Remove(message);
}

internal sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly TeamFlowDbContext _ctx;

    public AttachmentRepository(TeamFlowDbContext ctx, IUnitOfWork uow)
    {
        _ctx = ctx;
        UnitOfWork = uow;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<Attachment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Attachment>> ListForOwnerAsync(
        AttachmentOwner kind,
        Guid ownerId,
        CancellationToken ct = default
    ) =>
        await _ctx
            .Attachments.Where(a => a.OwnerKind == kind && a.OwnerId == ownerId)
            .ToListAsync(ct);

    public void Add(Attachment attachment) => _ctx.Attachments.Add(attachment);

    public void Remove(Attachment attachment) => _ctx.Attachments.Remove(attachment);
}

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly TeamFlowDbContext _ctx;

    public NotificationRepository(TeamFlowDbContext ctx, IUnitOfWork uow)
    {
        _ctx = ctx;
        UnitOfWork = uow;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Notification>> ListInboxAsync(
        Guid recipientId,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken ct = default
    )
    {
        var q = _ctx.Notifications.Where(n => n.RecipientId == recipientId);
        if (unreadOnly)
            q = q.Where(n => n.ReadAt == null);
        return await q.OrderByDescending(n => n.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(Guid recipientId, CancellationToken ct = default) =>
        _ctx.Notifications.CountAsync(n => n.RecipientId == recipientId && n.ReadAt == null, ct);

    public void Add(Notification notification) => _ctx.Notifications.Add(notification);
}

internal sealed class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly TeamFlowDbContext _ctx;

    public NotificationPreferenceRepository(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<NotificationPreference>> ListForUserAsync(
        Guid userId,
        CancellationToken ct = default
    ) =>
        await _ctx.NotificationPreferences.Where(p => p.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<NotificationPreference>> ListForUserAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken ct = default
    ) =>
        await _ctx
            .NotificationPreferences.Where(p =>
                p.UserId == userId && p.WorkspaceId == workspaceId
            )
            .ToListAsync(ct);

    public void Add(NotificationPreference preference) =>
        _ctx.NotificationPreferences.Add(preference);

    public void Remove(NotificationPreference preference) =>
        _ctx.NotificationPreferences.Remove(preference);

    public void RemoveRange(IEnumerable<NotificationPreference> preferences) =>
        _ctx.NotificationPreferences.RemoveRange(preferences);
}

internal sealed class ActivityEventRepository : IActivityEventRepository
{
    private readonly TeamFlowDbContext _ctx;

    public ActivityEventRepository(TeamFlowDbContext ctx, IUnitOfWork uow)
    {
        _ctx = ctx;
        UnitOfWork = uow;
    }

    public IUnitOfWork UnitOfWork { get; }

    public async Task<IReadOnlyList<ActivityEvent>> ListForWorkspaceAsync(
        Guid workspaceId,
        int skip,
        int take,
        CancellationToken ct = default
    ) =>
        await _ctx
            .ActivityEvents.Where(e => e.WorkspaceId == workspaceId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ActivityEvent>> ListForProjectAsync(
        Guid projectId,
        int skip,
        int take,
        CancellationToken ct = default
    ) =>
        await _ctx
            .ActivityEvents.Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public void Add(ActivityEvent activityEvent) => _ctx.ActivityEvents.Add(activityEvent);
}
