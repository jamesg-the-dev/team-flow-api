using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Workspaces.DTOs;

public sealed record WorkspaceMemberDto(
    Guid UserId,
    WorkspaceRole Role,
    string? Title,
    DateTimeOffset JoinedAt,
    Guid? InvitedBy
);

public sealed record WorkspaceInviteDto(
    Guid Id,
    string Email,
    WorkspaceRole Role,
    Guid InvitedBy,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset CreatedAt
);

/// <summary>Returned by <c>POST /workspaces/{id}/invites</c>. Token is shown exactly once.</summary>
public sealed record CreatedInviteDto(WorkspaceInviteDto Invite, string Token);

public sealed record AcceptInviteResultDto(
    Guid WorkspaceId,
    string WorkspaceSlug,
    string WorkspaceName,
    WorkspaceRole Role,
    bool MembershipCreated
);
