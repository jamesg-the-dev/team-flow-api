using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Workspaces.Commands.CreateTag;
using TeamFlow.Application.Features.Workspaces.Commands.UpdateTag;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceTags;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/workspaces/{workspaceId:guid}/tags")]
[Produces("application/json")]
public sealed class WorkspaceTagsController : ControllerBase
{
    private readonly ISender _sender;

    public WorkspaceTagsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TagDto>>> List(
        Guid workspaceId,
        CancellationToken ct
    ) => (await _sender.Send(new ListWorkspaceTagsQuery(workspaceId), ct)).ToActionResult();

    [HttpPost]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> Create(
        Guid workspaceId,
        [FromBody] CreateTagRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new CreateTagCommand(workspaceId, body.Name, body.ColorHex),
            ct
        );
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { workspaceId }, result.Value)
            : result.ToActionResult().Result!;
    }

    [HttpPatch("{tagId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update(
        Guid workspaceId,
        Guid tagId,
        [FromBody] UpdateTagRequest body,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(
                new UpdateTagCommand(workspaceId, tagId, body.Name, body.ColorHex),
                ct
            )
        ).ToActionResult();

    public sealed record CreateTagRequest(string Name, string? ColorHex);

    public sealed record UpdateTagRequest(string? Name, string? ColorHex);
}
