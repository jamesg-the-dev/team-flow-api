using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Common.Authorization;

/// <summary>
/// Small membership-based authorization helpers shared by Workspace / Project / Channel
/// handlers. Centralised so the role checks live in one place.
/// </summary>
internal static class WorkspaceAuthorization
{
    public static bool IsMember(Workspace workspace, Guid userId) =>
        workspace.Members.Any(m => m.UserId == userId);

    public static bool IsOwner(Workspace workspace, Guid userId) =>
        workspace.OwnerId == userId;

    public static bool IsOwnerOrAdmin(Workspace workspace, Guid userId) =>
        workspace.Members.Any(m =>
            m.UserId == userId
            && (m.Role == WorkspaceRole.Owner || m.Role == WorkspaceRole.Admin)
        );
}
