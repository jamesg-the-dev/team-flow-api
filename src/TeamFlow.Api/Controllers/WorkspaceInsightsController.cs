using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Dashboard.Queries.GetWorkspaceDashboard;
using TeamFlow.Application.Features.Search.Queries.SearchWorkspace;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceActivity;

namespace TeamFlow.Api.Controllers;

/// <summary>
/// Workspace-scoped read endpoints that span multiple aggregates: the home dashboard, the
/// activity feed and full-text search. Authorization is delegated to the underlying handlers.
/// </summary>
[ApiController]
[Authorize(Policy = "authenticated")]
[Produces("application/json")]
[Route("api/v1/workspaces/{workspaceId:guid}")]
public sealed class WorkspaceInsightsController : ControllerBase
{
    private readonly ISender _sender;

    public WorkspaceInsightsController(ISender sender) => _sender = sender;

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(WorkspaceDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceDashboardDto>> Dashboard(
        Guid workspaceId,
        CancellationToken ct
    ) => (await _sender.Send(new GetWorkspaceDashboardQuery(workspaceId), ct)).ToActionResult();

    [HttpGet("activity")]
    [ProducesResponseType(typeof(PagedResult<WorkspaceActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<WorkspaceActivityDto>>> Activity(
        Guid workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default
    )
    {
        var result = await _sender.Send(
            new ListWorkspaceActivityQuery(workspaceId, new PaginationRequest(page, pageSize)),
            ct
        );
        return result.ToActionResult();
    }

    /// <summary>
    /// Workspace full-text search. <c>q</c> uses websearch_to_tsquery syntax — quoted phrases,
    /// <c>-excluded</c>, and <c>OR</c> are all supported. <c>scope</c> defaults to
    /// <c>Tasks,Messages</c> when omitted; <c>take</c> is clamped to 50.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SearchResultDto>> Search(
        Guid workspaceId,
        [FromQuery(Name = "q")] string q,
        [FromQuery(Name = "scope")] SearchScope scope = SearchScope.All,
        [FromQuery(Name = "take")] int take = 20,
        CancellationToken ct = default
    )
    {
        var result = await _sender.Send(
            new SearchWorkspaceQuery(workspaceId, q ?? string.Empty, scope, take),
            ct
        );
        return result.ToActionResult();
    }
}
