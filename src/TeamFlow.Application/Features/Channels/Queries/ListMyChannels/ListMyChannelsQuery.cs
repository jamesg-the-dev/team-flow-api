using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Channels.DTOs;

namespace TeamFlow.Application.Features.Channels.Queries.ListMyChannels;

/// <summary>Lists every channel in <paramref name="WorkspaceId"/> the current user belongs to.</summary>
public sealed record ListMyChannelsQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<MyChannelDto>>;

public interface IListMyChannelsQueryService
{
    Task<IReadOnlyList<MyChannelDto>> ListAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct
    );
}

internal sealed class ListMyChannelsHandler
    : IQueryHandler<ListMyChannelsQuery, IReadOnlyList<MyChannelDto>>
{
    private readonly IListMyChannelsQueryService _svc;
    private readonly Common.Abstractions.ICurrentUser _currentUser;

    public ListMyChannelsHandler(
        IListMyChannelsQueryService svc,
        Common.Abstractions.ICurrentUser currentUser
    )
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MyChannelDto>>> Handle(
        ListMyChannelsQuery request,
        CancellationToken ct
    )
    {
        var rows = await _svc.ListAsync(request.WorkspaceId, _currentUser.RequireUserId(), ct);
        return Result.Success(rows);
    }
}
