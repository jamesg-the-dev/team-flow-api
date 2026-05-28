using Microsoft.EntityFrameworkCore;
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

    public NotificationDispatcher(INotificationRepository notifications, TeamFlowDbContext ctx)
    {
        _notifications = notifications;
        _ctx = ctx;
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
}
