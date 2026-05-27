using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Workspaces.Commands.CreateWorkspace;
using TeamFlow.Application.Features.Workspaces.Commands.DeleteWorkspace;
using TeamFlow.Application.Features.Workspaces.Commands.UpdateWorkspace;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.GetWorkspaceById;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/workspaces")]
[Produces("application/json")]
public sealed class WorkspacesController : ControllerBase
{
    private readonly ISender _sender;

    public WorkspacesController(ISender sender) => _sender = sender;

    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkspaceDto>> Create(
        [FromBody] CreateWorkspaceRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new CreateWorkspaceCommand(body.Slug, body.Name, body.LogoUrl),
            ct
        );
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { workspaceId = result.Value.Id }, result.Value)
            : result.ToActionResult().Result!;
    }

    [HttpGet("{workspaceId:guid}")]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkspaceDto>> Get(Guid workspaceId, CancellationToken ct) =>
        (await _sender.Send(new GetWorkspaceByIdQuery(workspaceId), ct)).ToActionResult();

    [HttpPatch("{workspaceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(
        Guid workspaceId,
        [FromBody] UpdateWorkspaceRequest body,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(
                new UpdateWorkspaceCommand(workspaceId, body.Name, body.LogoUrl),
                ct
            )
        ).ToActionResult();

    [HttpDelete("{workspaceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid workspaceId, CancellationToken ct) =>
        (await _sender.Send(new DeleteWorkspaceCommand(workspaceId), ct)).ToActionResult();

    public sealed record CreateWorkspaceRequest(string Slug, string Name, string? LogoUrl);

    public sealed record UpdateWorkspaceRequest(string Name, string? LogoUrl);
}
