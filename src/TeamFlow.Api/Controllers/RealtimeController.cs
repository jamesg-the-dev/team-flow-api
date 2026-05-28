using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Auth;
using TeamFlow.Api.Realtime;
using TeamFlow.Application.Common.Abstractions;

namespace TeamFlow.Api.Controllers;

/// <summary>
/// Mints a short-lived, hub-scoped access token for the SignalR realtime hub.
///
/// The caller authenticates here with their normal Supabase access token (Authorization
/// header). In return they receive a *separate* JWT, signed with a server-only key and
/// scoped exclusively to the hub audience. That hub token — and only that hub token — is
/// accepted by <see cref="RealtimeHub"/>. The Supabase access token is never accepted on
/// the WebSocket transport, so it cannot leak through query strings, proxy logs, or
/// referrer headers.
/// </summary>
[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/realtime")]
[Produces("application/json")]
public sealed class RealtimeController : ControllerBase
{
    private readonly IRealtimeTokenIssuer _issuer;
    private readonly ICurrentUser _currentUser;

    public RealtimeController(IRealtimeTokenIssuer issuer, ICurrentUser currentUser)
    {
        _issuer = issuer;
        _currentUser = currentUser;
    }

    public sealed record RealtimeConnectionDto(
        string HubUrl,
        string AccessToken,
        DateTimeOffset ExpiresAt
    );

    [HttpGet("token")]
    [ProducesResponseType(typeof(RealtimeConnectionDto), StatusCodes.Status200OK)]
    public ActionResult<RealtimeConnectionDto> Token()
    {
        var userId = _currentUser.RequireUserId();
        var (token, expiresAt) = _issuer.Issue(userId);
        return Ok(new RealtimeConnectionDto(RealtimeHub.Path, token, expiresAt));
    }
}
