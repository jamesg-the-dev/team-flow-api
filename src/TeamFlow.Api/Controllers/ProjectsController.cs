using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Projects.Commands.AddProjectMember;
using TeamFlow.Application.Features.Projects.Commands.ChangeProjectStatus;
using TeamFlow.Application.Features.Projects.Commands.CreateProject;
using TeamFlow.Application.Features.Projects.Commands.DeleteProject;
using TeamFlow.Application.Features.Projects.Commands.RemoveProjectMember;
using TeamFlow.Application.Features.Projects.Commands.UpdateProject;
using TeamFlow.Application.Features.Projects.Commands.UpdateProjectMember;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Queries.GetProjectById;
using TeamFlow.Application.Features.Projects.Queries.GetProjectStats;
using TeamFlow.Application.Features.Projects.Queries.GetProjectVelocity;
using TeamFlow.Application.Features.Projects.Queries.ListProjectActivity;
using TeamFlow.Application.Features.Projects.Queries.ListProjectMembers;
using TeamFlow.Application.Features.Projects.Queries.ListProjects;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/workspaces/{workspaceId:guid}/projects")]
[Produces("application/json")]
public sealed class ProjectsController : ControllerBase
{
    private readonly ISender _sender;

    public ProjectsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectSummaryDto>>> List(
        Guid workspaceId,
        [FromQuery] ProjectStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default
    )
    {
        var result = await _sender.Send(
            new ListProjectsQuery(
                workspaceId,
                status,
                search,
                new PaginationRequest(page, pageSize)
            ),
            ct
        );
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProjectByIdQuery(id), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> Create(
        Guid workspaceId,
        [FromBody] CreateProjectRequest body,
        CancellationToken ct
    )
    {
        var cmd = new CreateProjectCommand(
            workspaceId,
            body.Key,
            body.Name,
            body.Description,
            body.Priority,
            body.StartDate,
            body.DueDate,
            body.ColorHex
        );
        var result = await _sender.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { workspaceId, id = result.Value.Id }, result.Value)
            : result.ToActionResult().Result!;
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectRequest body,
        CancellationToken ct
    )
    {
        var cmd = new UpdateProjectCommand(
            id,
            body.Name,
            body.Description,
            body.Priority,
            body.StartDate,
            body.DueDate,
            body.ColorHex
        );
        var result = await _sender.Send(cmd, ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new ChangeProjectStatusCommand(id, body.Status), ct);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> AddMember(
        Guid id,
        [FromBody] AddMemberRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new AddProjectMemberCommand(id, body.UserId, body.Role),
            ct
        );
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProjectMemberDto>>> ListMembers(
        Guid id,
        CancellationToken ct
    ) => (await _sender.Send(new ListProjectMembersQuery(id), ct)).ToActionResult();

    [HttpPatch("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateMember(
        Guid id,
        Guid userId,
        [FromBody] UpdateMemberRequest body,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(
                new UpdateProjectMemberCommand(id, userId, body.Role),
                ct
            )
        ).ToActionResult();

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveMember(
        Guid id,
        Guid userId,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(new RemoveProjectMemberCommand(id, userId), ct)
        ).ToActionResult();

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct) =>
        (await _sender.Send(new DeleteProjectCommand(id), ct)).ToActionResult();

    [HttpGet("{id:guid}/activity")]
    [ProducesResponseType(typeof(PagedResult<ProjectActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<ProjectActivityDto>>> Activity(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default
    ) =>
        (
            await _sender.Send(
                new ListProjectActivityQuery(id, new PaginationRequest(page, pageSize)),
                ct
            )
        ).ToActionResult();

    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(ProjectStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectStatsDto>> Stats(Guid id, CancellationToken ct) =>
        (await _sender.Send(new GetProjectStatsQuery(id), ct)).ToActionResult();

    /// <summary>
    /// Weekly created vs. completed task counts for the trailing <paramref name="weeks"/> ISO weeks
    /// (Monday-anchored, UTC). Defaults to 12; clamped to a 52-week max.
    /// </summary>
    [HttpGet("{id:guid}/velocity")]
    [ProducesResponseType(typeof(IReadOnlyList<VelocityPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VelocityPointDto>>> Velocity(
        Guid id,
        [FromQuery] int weeks = 12,
        CancellationToken ct = default
    ) => (await _sender.Send(new GetProjectVelocityQuery(id, weeks), ct)).ToActionResult();

    // Request payloads kept local to controller; they map 1:1 to commands.
    public sealed record CreateProjectRequest(
        string Key,
        string Name,
        string? Description,
        PriorityLevel Priority,
        DateOnly? StartDate,
        DateOnly? DueDate,
        string? ColorHex
    );

    public sealed record UpdateProjectRequest(
        string Name,
        string? Description,
        PriorityLevel Priority,
        DateOnly? StartDate,
        DateOnly? DueDate,
        string? ColorHex
    );

    public sealed record ChangeStatusRequest(ProjectStatus Status);

    public sealed record AddMemberRequest(Guid UserId, ProjectMemberRole Role);

    public sealed record UpdateMemberRequest(ProjectMemberRole Role);
}
