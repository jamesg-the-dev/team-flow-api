using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Notifications.Services;

/// <summary>
/// Application-side fan-out service for emitting in-app notifications. Implemented in
/// Infrastructure; it consults <see cref="NotificationPreference"/>s for the
/// <see cref="DeliveryChannel.InApp"/> channel and only writes when enabled (or no preference
/// is recorded — default-on).
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Enqueues a notification for a single recipient. The notification is persisted within
    /// the current unit-of-work; callers do not need to manage saving.
    /// </summary>
    Task NotifyAsync(NotificationRequest request, CancellationToken ct);

    /// <summary>Convenience fan-out for delivering the same notification to multiple recipients.</summary>
    Task NotifyManyAsync(IEnumerable<NotificationRequest> requests, CancellationToken ct);
}

public sealed record NotificationRequest(
    Guid WorkspaceId,
    Guid RecipientId,
    NotificationKind Kind,
    string Title,
    Guid? ActorId = null,
    string? Body = null,
    string? TargetKind = null,
    Guid? TargetId = null,
    string? Url = null
);
