using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Invites.Commands.AcceptInvite;
using TeamFlow.Application.Features.Workspaces.DTOs;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/invites")]
[Produces("application/json")]
public sealed class InvitesController : ControllerBase
{
    private readonly ISender _sender;

    public InvitesController(ISender sender) => _sender = sender;

    [HttpPost("accept")]
    [ProducesResponseType(typeof(AcceptInviteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AcceptInviteResultDto>> Accept(
        [FromBody] AcceptInviteRequest body,
        CancellationToken ct
    ) => (await _sender.Send(new AcceptInviteCommand(body.Token), ct)).ToActionResult();

    public sealed record AcceptInviteRequest(string Token);
}
