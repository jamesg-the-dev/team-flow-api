using TeamFlow.Domain.Discussions;

namespace TeamFlow.Application.Common.Authorization;

/// <summary>Channel-scope authorization helpers used by command handlers.</summary>
internal static class ChannelAuthorization
{
    public static bool IsMember(Channel channel, Guid userId) =>
        channel.Members.Any(m => m.UserId == userId);

    /// <summary>
    /// True when the user is the channel creator. Workspace Owner / Admin is checked
    /// separately by handlers via <see cref="TeamFlow.Domain.Workspaces.IWorkspaceRepository.IsOwnerOrAdminAsync"/>.
    /// </summary>
    public static bool IsCreator(Channel channel, Guid userId) => channel.CreatedBy == userId;
}
