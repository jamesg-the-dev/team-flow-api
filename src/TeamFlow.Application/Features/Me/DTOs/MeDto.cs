using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Identity;

namespace TeamFlow.Application.Features.Me.DTOs;

/// <summary>Projection of the current Supabase identity (built from the access-token claims).</summary>
public sealed record MeDto(
    Guid Id,
    string? Email,
    bool EmailVerified,
    string? FullName,
    string? AvatarUrl
);

/// <summary>Projection of a row in the <c>profiles</c> table.</summary>
public sealed record ProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? DisplayName,
    string? AvatarPath,
    string? Bio,
    string Timezone,
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
)
{
    public static ProfileDto From(Profile p) =>
        new(
            p.Id,
            p.UserId,
            p.FullName,
            p.DisplayName,
            p.AvatarPath,
            p.Bio,
            p.Timezone,
            p.Locale,
            p.CreatedAt,
            p.UpdatedAt
        );
}

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
