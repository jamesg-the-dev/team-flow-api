using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamFlow.Api.Auth;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Api.Realtime;

/// <summary>
/// Single hub for all realtime traffic. Authentication uses the dedicated
/// <see cref="RealtimeTokenOptions.Scheme"/> JwtBearer scheme — Supabase access tokens are
/// NOT accepted here. Clients obtain a short-lived hub token from
/// <c>GET /api/v1/realtime/token</c> and pass it as the SignalR <c>access_token</c>.
///
/// On connect each caller is auto-joined to <c>user:{userId}</c> so personal events
/// (notifications, mentions, etc.) arrive without any client-side subscription. Per-channel
/// and per-workspace groups are opt-in via <see cref="JoinChannel"/> /
/// <see cref="JoinWorkspace"/> and are authorization-checked server-side — clients cannot
/// listen in on a channel they're not a member of by guessing the id.
/// </summary>
[Authorize(AuthenticationSchemes = RealtimeTokenOptions.Scheme, Policy = "realtime-hub")]
public sealed class RealtimeHub : Hub<IRealtimeClient>
{
    public const string Path = "/hubs/realtime";

    private readonly ICurrentUser _currentUser;
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;

    public RealtimeHub(
        ICurrentUser currentUser,
        IChannelRepository channels,
        IWorkspaceRepository workspaces
    )
    {
        _currentUser = currentUser;
        _channels = channels;
        _workspaces = workspaces;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        if (userId is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.User(userId.Value));
        await base.OnConnectedAsync();
    }

    public async Task JoinChannel(Guid channelId)
    {
        var userId = RequireUserId();
        if (!await _channels.IsMemberAsync(channelId, userId, Context.ConnectionAborted))
            throw new HubException("Not a member of this channel.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Channel(channelId),
            Context.ConnectionAborted
        );
    }

    public Task LeaveChannel(Guid channelId) =>
        Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Channel(channelId),
            Context.ConnectionAborted
        );

    public async Task JoinWorkspace(Guid workspaceId)
    {
        var userId = RequireUserId();
        if (!await _workspaces.IsMemberAsync(workspaceId, userId, Context.ConnectionAborted))
            throw new HubException("Not a member of this workspace.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Workspace(workspaceId),
            Context.ConnectionAborted
        );
    }

    public Task LeaveWorkspace(Guid workspaceId) =>
        Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.Workspace(workspaceId),
            Context.ConnectionAborted
        );

    private Guid? ResolveUserId()
    {
        // The hub is hit before the MVC ICurrentUser pipeline runs in the same way, so we
        // resolve directly from the ClaimsPrincipal as a safety net.
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private Guid RequireUserId() =>
        ResolveUserId() ?? throw new HubException("Unauthenticated.");
}
