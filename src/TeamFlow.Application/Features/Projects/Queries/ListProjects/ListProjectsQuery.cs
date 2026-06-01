using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Projects.Queries.ListProjects;

public sealed record ListProjectsQuery(
    Guid WorkspaceId,
    ProjectStatus? Status,
    bool ActiveOnly,
    string? Search,
    PaginationRequest Pagination
) : IQuery<PagedResult<ProjectSummaryDto>>;

/// <summary>
/// Read-side query. Handler is implemented in the Infrastructure project (it uses EF projection
/// directly against the read model and bypasses the aggregate).
/// </summary>
public interface IListProjectsQueryService
{
    Task<PagedResult<ProjectSummaryDto>> ExecuteAsync(
        ListProjectsQuery query,
        CancellationToken ct
    );
}

internal sealed class ListProjectsHandler
    : IQueryHandler<ListProjectsQuery, PagedResult<ProjectSummaryDto>>
{
    private readonly IListProjectsQueryService _service;

    public ListProjectsHandler(IListProjectsQueryService service) => _service = service;

    public async Task<Result<PagedResult<ProjectSummaryDto>>> Handle(
        ListProjectsQuery request,
        CancellationToken ct
    )
    {
        var page = await _service.ExecuteAsync(request, ct);
        return Result.Success(page);
    }
}
