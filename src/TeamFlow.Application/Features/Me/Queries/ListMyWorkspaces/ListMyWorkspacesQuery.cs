using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Me.DTOs;

namespace TeamFlow.Application.Features.Me.Queries.ListMyWorkspaces;

public sealed record ListMyWorkspacesQuery : IQuery<IReadOnlyList<MyWorkspaceDto>>;

/// <summary>Read-side service; implementation lives in Infrastructure and projects directly.</summary>
public interface IListMyWorkspacesQueryService
{
    Task<IReadOnlyList<MyWorkspaceDto>> ExecuteAsync(Guid userId, CancellationToken ct);
}

internal sealed class ListMyWorkspacesHandler
    : IQueryHandler<ListMyWorkspacesQuery, IReadOnlyList<MyWorkspaceDto>>
{
    private readonly IListMyWorkspacesQueryService _service;
    private readonly ICurrentUser _currentUser;

    public ListMyWorkspacesHandler(
        IListMyWorkspacesQueryService service,
        ICurrentUser currentUser
    )
    {
        _service = service;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MyWorkspaceDto>>> Handle(
        ListMyWorkspacesQuery request,
        CancellationToken ct
    )
    {
        var items = await _service.ExecuteAsync(_currentUser.RequireUserId(), ct);
        return Result.Success(items);
    }
}
