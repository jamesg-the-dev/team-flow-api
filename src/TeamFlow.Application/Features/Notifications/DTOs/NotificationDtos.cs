using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Notifications.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? ActorId,
    NotificationKind Kind,
    string Title,
    string? Body,
    string? TargetKind,
    Guid? TargetId,
    string? Url,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt
);

public sealed record NotificationPreferenceDto(
    NotificationKind Kind,
    DeliveryChannel Channel,
    bool Enabled
);
