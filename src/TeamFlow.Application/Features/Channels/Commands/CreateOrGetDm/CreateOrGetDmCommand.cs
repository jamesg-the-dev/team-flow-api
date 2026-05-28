using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Channels.DTOs;
using TeamFlow.Domain.Discussions;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Channels.Commands.CreateOrGetDm;

/// <summary>
/// Idempotently creates (or returns) the 1-1 direct-message channel between the caller and
/// <paramref name="OtherUserId"/> inside <paramref name="WorkspaceId"/>.
/// </summary>
public sealed record CreateOrGetDmCommand(Guid WorkspaceId, Guid OtherUserId) : ICommand<ChannelDto>;

internal sealed class CreateOrGetDmHandler : ICommandHandler<CreateOrGetDmCommand, ChannelDto>
{
    private readonly IChannelRepository _channels;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public CreateOrGetDmHandler(
        IChannelRepository channels,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _channels = channels;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<ChannelDto>> Handle(CreateOrGetDmCommand request, CancellationToken ct)
    {
        var actor = _currentUser.RequireUserId();
        if (actor == request.OtherUserId)
            return Error.Validation("Cannot start a direct-message channel with yourself.");

        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, actor, ct))
            return Error.Forbidden("You are not a member of this workspace.");
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, request.OtherUserId, ct))
            return Error.Validation("The other user is not a member of this workspace.");

        var existing = await _channels.FindDirectChannelAsync(
            request.WorkspaceId,
            actor,
            request.OtherUserId,
            ct
        );

        Channel channel;
        if (existing is not null)
        {
            channel = existing;
        }
        else
        {
            // Channel name must be unique per workspace; use a deterministic, low-collision token
            // derived from the sorted user-pair so concurrent creates idempotently resolve.
            var (a, b) =
                actor.CompareTo(request.OtherUserId) <= 0
                    ? (actor, request.OtherUserId)
                    : (request.OtherUserId, actor);
            var name = $"dm-{a:n}-{b:n}";

            channel = Channel.Create(
                request.WorkspaceId,
                name,
                ChannelType.Direct,
                createdBy: actor,
                topic: null
            );
            channel.Join(request.OtherUserId);
            _channels.Add(channel);
        }

        return new ChannelDto(
            channel.Id,
            channel.WorkspaceId,
            channel.Name,
            channel.Topic,
            channel.Type,
            channel.CreatedBy,
            channel.CreatedAt,
            channel.Members.Count
        );
    }
}
