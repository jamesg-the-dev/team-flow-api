using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Search.Queries.SearchWorkspace;

public sealed record TaskHitDto(
    Guid Id,
    Guid ProjectId,
    int Number,
    string Title,
    string Snippet,
    DateTimeOffset CreatedAt,
    double Rank
);

public sealed record MessageHitDto(
    Guid Id,
    Guid ChannelId,
    Guid AuthorId,
    string Snippet,
    DateTimeOffset CreatedAt,
    double Rank
);

public sealed record SearchResultDto(
    IReadOnlyList<TaskHitDto> Tasks,
    IReadOnlyList<MessageHitDto> Messages
);

[Flags]
public enum SearchScope
{
    None = 0,
    Tasks = 1,
    Messages = 2,
    All = Tasks | Messages,
}

/// <summary>
/// Full-text search across tasks and channel messages in a workspace, restricted to artifacts
/// the caller can see (workspace member for tasks; channel member for messages).
/// </summary>
public sealed record SearchWorkspaceQuery(Guid WorkspaceId, string Query, SearchScope Scope, int Take)
    : IQuery<SearchResultDto>;

public interface ISearchWorkspaceQueryService
{
    Task<SearchResultDto> ExecuteAsync(
        Guid workspaceId,
        Guid userId,
        string query,
        SearchScope scope,
        int take,
        CancellationToken ct
    );
}

internal sealed class SearchWorkspaceHandler : IQueryHandler<SearchWorkspaceQuery, SearchResultDto>
{
    private const int MaxTake = 50;
    private const int DefaultTake = 20;

    private readonly ISearchWorkspaceQueryService _svc;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public SearchWorkspaceHandler(
        ISearchWorkspaceQueryService svc,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _svc = svc;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<SearchResultDto>> Handle(
        SearchWorkspaceQuery request,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return Error.Validation("Query 'q' is required.");

        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");

        var scope = request.Scope == SearchScope.None ? SearchScope.All : request.Scope;
        var take = request.Take <= 0 ? DefaultTake : Math.Min(request.Take, MaxTake);

        return await _svc.ExecuteAsync(
            request.WorkspaceId,
            userId,
            request.Query.Trim(),
            scope,
            take,
            ct
        );
    }
}
