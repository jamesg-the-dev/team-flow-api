using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Features.Me.Commands.UpdateMyNotificationPreferences;
using TeamFlow.Application.Features.Me.Commands.UpsertMyProfile;
using TeamFlow.Application.Features.Me.DTOs;
using TeamFlow.Application.Features.Me.Queries.GetMe;
using TeamFlow.Application.Features.Me.Queries.GetMyNotificationPreferences;
using TeamFlow.Application.Features.Me.Queries.GetMyProfile;
using TeamFlow.Application.Features.Me.Queries.ListMyWorkspaces;

namespace TeamFlow.Api.Controllers;

/// <summary>Endpoints scoped to the calling Supabase user (the JWT <c>sub</c>).</summary>
[ApiController]
[Authorize(Policy = "authenticated")]
[Route("api/v1/me")]
[Produces("application/json")]
public sealed class MeController : ControllerBase
{
    private readonly ISender _sender;

    public MeController(ISender sender) => _sender = sender;

    /// <summary>Returns the current user's identity claims (sourced from the access token).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeDto>> Get(CancellationToken ct) =>
        (await _sender.Send(new GetMeQuery(), ct)).ToActionResult();

    /// <summary>
    /// Returns the caller's TeamFlow profile. Responds with <c>200</c> and a <c>null</c> body
    /// when no profile has been created yet — clients should follow up with <c>PUT /me/profile</c>.
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileDto?>> GetProfile(CancellationToken ct) =>
        (await _sender.Send(new GetMyProfileQuery(), ct)).ToActionResult();

    /// <summary>Creates the caller's profile if missing, or updates it if it already exists.</summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> UpsertProfile(
        [FromBody] UpsertMyProfileCommand body,
        CancellationToken ct
    ) => (await _sender.Send(body, ct)).ToActionResult();

    /// <summary>Lists every workspace the current user belongs to.</summary>
    [HttpGet("workspaces")]
    [ProducesResponseType(typeof(IReadOnlyList<MyWorkspaceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyWorkspaceDto>>> Workspaces(
        CancellationToken ct
    ) => (await _sender.Send(new ListMyWorkspacesQuery(), ct)).ToActionResult();

    /// <summary>
    /// Returns the caller's notification-preference rows. When <paramref name="workspaceId"/>
    /// is provided, the result is scoped to that workspace.
    /// </summary>
    [HttpGet("notification-preferences")]
    [ProducesResponseType(
        typeof(IReadOnlyList<MyNotificationPreferenceDto>),
        StatusCodes.Status200OK
    )]
    public async Task<
        ActionResult<IReadOnlyList<MyNotificationPreferenceDto>>
    > NotificationPreferences([FromQuery] Guid? workspaceId, CancellationToken ct) =>
        (
            await _sender.Send(new GetMyNotificationPreferencesQuery(workspaceId), ct)
        ).ToActionResult();

    /// <summary>Replaces the caller's notification preferences for a workspace.</summary>
    [HttpPut("notification-preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateNotificationPreferencesRequest body,
        CancellationToken ct
    ) =>
        (
            await _sender.Send(
                new UpdateMyNotificationPreferencesCommand(body.WorkspaceId, body.Items),
                ct
            )
        ).ToActionResult();

    public sealed record UpdateNotificationPreferencesRequest(
        Guid WorkspaceId,
        IReadOnlyList<NotificationPreferenceInput> Items
    );
}
