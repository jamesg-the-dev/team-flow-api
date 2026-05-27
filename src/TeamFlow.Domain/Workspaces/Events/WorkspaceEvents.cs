using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Workspaces.Events;

public sealed record WorkspaceCreated(Guid WorkspaceId, Guid OwnerId, string Slug) : DomainEvent;

public sealed record WorkspaceMemberAdded(Guid WorkspaceId, Guid UserId, WorkspaceRole Role)
    : DomainEvent;

public sealed record WorkspaceMemberRemoved(Guid WorkspaceId, Guid UserId) : DomainEvent;

public sealed record WorkspaceOwnershipTransferred(Guid WorkspaceId, Guid FromUserId, Guid ToUserId)
    : DomainEvent;

public sealed record WorkspaceInviteIssued(Guid WorkspaceId, Guid InviteId, string Email)
    : DomainEvent;
