using TeamFlow.Domain.Enums;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Domain.Workspaces;

/// <summary>Membership row inside a Workspace aggregate. Composite PK (workspace_id, user_id).</summary>
public sealed class WorkspaceMember
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public string? Title { get; internal set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public Guid? InvitedBy { get; private set; }

    private WorkspaceMember() { }

    internal WorkspaceMember(Guid workspaceId, Guid userId, WorkspaceRole role, DateTimeOffset joinedAt, Guid? invitedBy)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
        InvitedBy = invitedBy;
    }

    internal void ChangeRole(WorkspaceRole role) => Role = role;
}
