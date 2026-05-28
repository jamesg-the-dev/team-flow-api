using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using TeamFlow.Application.Features.Search.Queries.SearchWorkspace;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

/// <summary>
/// Workspace-scoped full-text search across tasks and messages. Uses Postgres GIN-indexed
/// <c>search_tsv</c> shadow columns plus <c>websearch_to_tsquery</c> so callers get
/// Google-style query syntax (quoted phrases, <c>-exclusions</c>, <c>OR</c>).
/// </summary>
internal sealed class SearchWorkspaceQueryService : ISearchWorkspaceQueryService
{
    private const string FtsConfig = "english";
    private const int SnippetLength = 200;

    private readonly TeamFlowDbContext _ctx;

    public SearchWorkspaceQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<SearchResultDto> ExecuteAsync(
        Guid workspaceId,
        Guid userId,
        string query,
        SearchScope scope,
        int take,
        CancellationToken ct
    )
    {
        var tasks = scope.HasFlag(SearchScope.Tasks)
            ? await SearchTasksAsync(workspaceId, query, take, ct)
            : Array.Empty<TaskHitDto>();

        var messages = scope.HasFlag(SearchScope.Messages)
            ? await SearchMessagesAsync(workspaceId, userId, query, take, ct)
            : Array.Empty<MessageHitDto>();

        return new SearchResultDto(tasks, messages);
    }

    private async Task<IReadOnlyList<TaskHitDto>> SearchTasksAsync(
        Guid workspaceId,
        string query,
        int take,
        CancellationToken ct
    )
    {
        var rows = await (
            from t in _ctx.Tasks.AsNoTracking()
            where
                t.DeletedAt == null
                && _ctx.Projects.Any(p => p.Id == t.ProjectId && p.WorkspaceId == workspaceId)
                && EF.Property<NpgsqlTsVector>(t, "search_tsv")
                    .Matches(EF.Functions.WebSearchToTsQuery(FtsConfig, query))
            let rank = EF.Property<NpgsqlTsVector>(t, "search_tsv")
                .Rank(EF.Functions.WebSearchToTsQuery(FtsConfig, query))
            orderby rank descending, t.CreatedAt descending
            select new
            {
                t.Id,
                t.ProjectId,
                t.Number,
                t.Title,
                t.Description,
                t.CreatedAt,
                Rank = rank,
            }
        )
            .Take(take)
            .ToListAsync(ct);

        return rows
            .Select(r => new TaskHitDto(
                r.Id,
                r.ProjectId,
                r.Number,
                r.Title,
                BuildSnippet(r.Description ?? r.Title),
                r.CreatedAt,
                r.Rank
            ))
            .ToList();
    }

    private async Task<IReadOnlyList<MessageHitDto>> SearchMessagesAsync(
        Guid workspaceId,
        Guid userId,
        string query,
        int take,
        CancellationToken ct
    )
    {
        var rows = await (
            from m in _ctx.Messages.AsNoTracking()
            where
                m.DeletedAt == null
                && _ctx.Channels.Any(c => c.Id == m.ChannelId && c.WorkspaceId == workspaceId)
                // Visibility: caller must be a member of the channel hosting the message.
                && _ctx.ChannelMembers.Any(cm => cm.ChannelId == m.ChannelId && cm.UserId == userId)
                && EF.Property<NpgsqlTsVector>(m, "search_tsv")
                    .Matches(EF.Functions.WebSearchToTsQuery(FtsConfig, query))
            let rank = EF.Property<NpgsqlTsVector>(m, "search_tsv")
                .Rank(EF.Functions.WebSearchToTsQuery(FtsConfig, query))
            orderby rank descending, m.CreatedAt descending
            select new
            {
                m.Id,
                m.ChannelId,
                m.AuthorId,
                m.Body,
                m.CreatedAt,
                Rank = rank,
            }
        )
            .Take(take)
            .ToListAsync(ct);

        return rows
            .Select(r => new MessageHitDto(
                r.Id,
                r.ChannelId,
                r.AuthorId,
                BuildSnippet(r.Body),
                r.CreatedAt,
                r.Rank
            ))
            .ToList();
    }

    private static string BuildSnippet(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        return source.Length <= SnippetLength ? source : source[..SnippetLength] + "…";
    }
}
