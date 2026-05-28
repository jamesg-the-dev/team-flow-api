using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Me.DTOs;

/// <summary>Projection of the current Supabase identity (built from the access-token claims).</summary>
public sealed record MeDto(
    Guid Id,
    string? Email,
    bool EmailVerified,
    string? FullName,
    string? AvatarUrl
);

/// <summary>One row of <c>GET /me/workspaces</c>.</summary>
public sealed record MyWorkspaceDto(
    Guid Id,
    string Slug,
    string Name,
    string? LogoUrl,
    WorkspaceRole Role,
    DateTimeOffset JoinedAt
);

public sealed record MyNotificationPreferenceDto(
    Guid WorkspaceId,
    NotificationKind Kind,
    DeliveryChannel Channel,
    bool Enabled
);
