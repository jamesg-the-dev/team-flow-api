using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;
using TeamFlow.Application.Features.Workspaces.Commands.UpdateWorkspaceMember;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Application.Features.Workspaces.Queries.ListWorkspaceMembers;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/workspaces/{workspaceId:guid}/members")]
[Produces("application/json")]
public sealed class WorkspaceMembersController : ControllerBase
{
    private readonly ISender _sender;

    public WorkspaceMembersController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<WorkspaceMemberDto>>> List(
        Guid workspaceId,
        CancellationToken ct
    ) =>
        (await _sender.Send(new ListWorkspaceMembersQuery(workspaceId), ct)).ToActionResult();

    [HttpPatch("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(
        Guid workspaceId,
        Guid userId,
        [FromBody] UpdateWorkspaceMemberRequest body,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(
                new UpdateWorkspaceMemberCommand(
                    workspaceId,
                    userId,
                    body.Role,
                    body.Title,
                    body.ClearTitle
                ),
                ct
            )
        ).ToActionResult();

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(new RemoveWorkspaceMemberCommand(workspaceId, userId), ct)
        ).ToActionResult();

    public sealed record UpdateWorkspaceMemberRequest(
        WorkspaceRole? Role,
        string? Title,
        bool ClearTitle = false
    );
}
