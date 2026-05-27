using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Projects.Commands.AddProjectMember;
using TeamFlow.Application.Features.Projects.Commands.ChangeProjectStatus;
using TeamFlow.Application.Features.Projects.Commands.CreateProject;
using TeamFlow.Application.Features.Projects.Commands.UpdateProject;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Application.Features.Projects.Queries.GetProjectById;
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
}
