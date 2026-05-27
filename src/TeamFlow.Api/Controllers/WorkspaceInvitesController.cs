using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Workspaces.Commands.CreateInvite;
using TeamFlow.Application.Features.Workspaces.Commands.RevokeInvite;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceInvites;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/workspaces/{workspaceId:guid}/invites")]
[Produces("application/json")]
public sealed class WorkspaceInvitesController : ControllerBase
{
    private readonly ISender _sender;

    public WorkspaceInvitesController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceInviteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<WorkspaceInviteDto>>> List(
        Guid workspaceId,
        [FromQuery] bool includeExpired = false,
        CancellationToken ct = default
    ) =>
        (
            await _sender.Send(new ListWorkspaceInvitesQuery(workspaceId, includeExpired), ct)
        ).ToActionResult();

    [HttpPost]
    [ProducesResponseType(typeof(CreatedInviteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedInviteDto>> Create(
        Guid workspaceId,
        [FromBody] CreateInviteRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new CreateInviteCommand(
                workspaceId,
                body.Email,
                body.Role,
                body.ExpiryDays is { } d ? TimeSpan.FromDays(d) : null
            ),
            ct
        );
        return result.IsSuccess
            ? CreatedAtAction(nameof(List), new { workspaceId }, result.Value)
            : result.ToActionResult().Result!;
    }

    [HttpDelete("{inviteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Revoke(
        Guid workspaceId,
        Guid inviteId,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(new RevokeInviteCommand(workspaceId, inviteId), ct)
        ).ToActionResult();

    public sealed record CreateInviteRequest(string Email, WorkspaceRole Role, int? ExpiryDays);
}
