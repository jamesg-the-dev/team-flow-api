using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Api.Middleware;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Features.Notifications.Commands.MarkAllRead;
using TeamFlow.Application.Features.Notifications.Commands.MarkRead;
using TeamFlow.Application.Features.Notifications.Commands.UpdatePreferences;
using TeamFlow.Application.Features.Notifications.DTOs;
using TeamFlow.Application.Features.Notifications.Queries.GetUnreadCount;
using TeamFlow.Application.Features.Notifications.Queries.ListInbox;
using TeamFlow.Application.Features.Notifications.Queries.ListPreferences;

namespace TeamFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = "authenticated")]
[Produces("application/json")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    /// <summary>Inbox listing across all workspaces, newest-first.</summary>
    [HttpGet("api/v1/notifications")]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default
    )
    {
        var result = await _sender.Send(
            new ListInboxQuery(unreadOnly, new PaginationRequest(page, pageSize)),
            ct
        );
        return result.ToActionResult();
    }

    [HttpGet("api/v1/notifications/unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken ct)
    {
        var result = await _sender.Send(new GetUnreadCountQuery(), ct);
        return result.ToActionResult();
    }

    [HttpPost("api/v1/notifications/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.ToActionResult();
    }

    /// <summary>Marks every unread notification as read. Returns the number of rows affected.</summary>
    [HttpPost("api/v1/notifications/read-all")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> MarkAllRead(CancellationToken ct)
    {
        var result = await _sender.Send(new MarkAllNotificationsReadCommand(), ct);
        return result.ToActionResult();
    }

    // ---- preferences ----------------------------------------------------------------------

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/notification-preferences")]
    [ProducesResponseType(
        typeof(IReadOnlyList<NotificationPreferenceDto>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NotificationPreferenceDto>>> ListPreferences(
        Guid workspaceId,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(new ListPreferencesQuery(workspaceId), ct);
        return result.ToActionResult();
    }

    [HttpPut("api/v1/workspaces/{workspaceId:guid}/notification-preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdatePreferences(
        Guid workspaceId,
        [FromBody] UpdatePreferencesRequest body,
        CancellationToken ct
    )
    {
        var result = await _sender.Send(
            new UpdatePreferencesCommand(workspaceId, body.Preferences),
            ct
        );
        return result.ToActionResult();
    }
}

public sealed record UpdatePreferencesRequest(IReadOnlyList<PreferenceItem> Preferences);
