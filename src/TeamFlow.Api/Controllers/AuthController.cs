using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Auth.Commands.Bootstrap;
using TeamFlow.Application.Features.Auth.DTOs;

namespace TeamFlow.Api.Controllers;

/// <summary>
/// Auth-related endpoints that complement Supabase Auth. Sign-in/up/sign-out
/// flows themselves run on the client via <c>supabase-js</c>; this controller
/// covers server-side provisioning that must happen after authentication.
/// </summary>
[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>
    /// First-login provisioning. Idempotent — returns the caller's default workspace,
    /// creating a personal one when no membership exists yet.
    /// </summary>
    [HttpPost("bootstrap")]
    [ProducesResponseType(typeof(BootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BootstrapDto>> Bootstrap(CancellationToken ct) =>
        (await _sender.Send(new BootstrapCommand(), ct)).ToActionResult();
}
