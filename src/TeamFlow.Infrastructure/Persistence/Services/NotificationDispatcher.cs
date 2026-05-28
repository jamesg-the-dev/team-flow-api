using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Realtime;
using TeamFlow.Application.Features.Notifications.DTOs;
using TeamFlow.Application.Features.Notifications.Services;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Infrastructure.Persistence.Services;

/// <summary>
/// In-app delivery only. Consults <see cref="NotificationPreference"/> for the
/// <c>(recipient, workspace, kind, InApp)</c> row; if no row exists the user is considered
/// opted-in (default-on). Notifications are added to the current EF unit-of-work so they
/// commit atomically with the originating command.
/// </summary>
internal sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationRepository _notifications;
    private readonly TeamFlowDbContext _ctx;
    private readonly IRealtimePublishQueue _realtime;

    public NotificationDispatcher(
        INotificationRepository notifications,
        TeamFlowDbContext ctx,
        IRealtimePublishQueue realtime
    )
    {
        _notifications = notifications;
        _ctx = ctx;
        _realtime = realtime;
    }

    public async Task NotifyAsync(NotificationRequest request, CancellationToken ct)
    {
        if (request.RecipientId == Guid.Empty)
            return;
        if (await IsSuppressedAsync(request.RecipientId, request.WorkspaceId, request.Kind, ct))
            return;

        var n = Notification.Create(
            request.WorkspaceId,
            request.RecipientId,
            request.Kind,
            request.Title,
            request.ActorId,
            request.Body,
            request.TargetKind,
            request.TargetId,
            request.Url
        );
        _notifications.Add(n);
        EnqueueRealtime(n);
    }

    public async Task NotifyManyAsync(
        IEnumerable<NotificationRequest> requests,
        CancellationToken ct
    )
    {
        // Batch-load the (workspace, kind) → enabled flag for InApp once per fan-out call.
        var list = requests.ToList();
        if (list.Count == 0) return;

        var keys = list.Select(r => new { r.RecipientId, r.WorkspaceId, r.Kind }).Distinct().ToList();
        // Pull every potentially-relevant preference row in one query.
        var recipients = keys.Select(k => k.RecipientId).Distinct().ToArray();
        var workspaces = keys.Select(k => k.WorkspaceId).Distinct().ToArray();
        var prefs = await _ctx
            .NotificationPreferences.AsNoTracking()
            .Where(p =>
                recipients.Contains(p.UserId)
                && workspaces.Contains(p.WorkspaceId)
                && p.Channel == DeliveryChannel.InApp
            )
            .ToListAsync(ct);

        var index = prefs.ToDictionary(p => (p.UserId, p.WorkspaceId, p.Kind), p => p.Enabled);

        foreach (var req in list)
        {
            if (req.RecipientId == Guid.Empty)
                continue;
            if (index.TryGetValue((req.RecipientId, req.WorkspaceId, req.Kind), out var enabled)
                && !enabled)
            {
                continue;
            }

            _notifications.Add(
                Notification.Create(
                    req.WorkspaceId,
                    req.RecipientId,
                    req.Kind,
                    req.Title,
                    req.ActorId,
                    req.Body,
                    req.TargetKind,
                    req.TargetId,
                    req.Url
                )
            );
            // Use the request fields directly — the entity isn't materialized again until
            // SaveChanges runs, but the realtime payload only needs the shape clients render.
            EnqueueRealtime(req);
        }
    }

    private Task<bool> IsSuppressedAsync(
        Guid recipientId,
        Guid workspaceId,
        NotificationKind kind,
        CancellationToken ct
    ) =>
        _ctx
            .NotificationPreferences.AsNoTracking()
            .Where(p =>
                p.UserId == recipientId
                && p.WorkspaceId == workspaceId
                && p.Kind == kind
                && p.Channel == DeliveryChannel.InApp
                && !p.Enabled
            )
            .AnyAsync(ct);

    private void EnqueueRealtime(Notification n) =>
        _realtime.Enqueue(
            new RealtimeEvent(
                RealtimeTarget.User,
                n.RecipientId,
                RealtimeEvents.NotificationCreated,
                new NotificationDto(
                    n.Id,
                    n.WorkspaceId,
                    n.ActorId,
                    n.Kind,
                    n.Title,
                    n.Body,
                    n.TargetKind,
                    n.TargetId,
                    n.Url,
                    n.ReadAt,
                    n.CreatedAt
                )
            )
        );

    private void EnqueueRealtime(NotificationRequest r) =>
        _realtime.Enqueue(
            new RealtimeEvent(
                RealtimeTarget.User,
                r.RecipientId,
                RealtimeEvents.NotificationCreated,
                new
                {
                    workspaceId = r.WorkspaceId,
                    actorId = r.ActorId,
                    kind = r.Kind,
                    title = r.Title,
                    body = r.Body,
                    targetKind = r.TargetKind,
                    targetId = r.TargetId,
                    url = r.Url,
                }
            )
        );
}
