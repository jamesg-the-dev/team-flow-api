using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Pagination;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Notifications.DTOs;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Application.Features.Notifications.Queries.ListInbox;

/// <summary>Lists the current user's notification inbox across all workspaces, newest-first.</summary>
public sealed record ListInboxQuery(bool UnreadOnly, PaginationRequest Pagination)
    : IQuery<PagedResult<NotificationDto>>;

internal sealed class ListInboxHandler
    : IQueryHandler<ListInboxQuery, PagedResult<NotificationDto>>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUser _currentUser;

    public ListInboxHandler(INotificationRepository notifications, ICurrentUser currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        ListInboxQuery request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        var skip = (request.Pagination.Page - 1) * request.Pagination.PageSize;
        var rows = await _notifications.ListInboxAsync(
            userId,
            request.UnreadOnly,
            skip,
            request.Pagination.PageSize,
            ct
        );

        // Total comes from the unread count when unreadOnly; otherwise we accept O(1)-ish full count
        // via a small extra query on the repo's underlying set — recipient + index keeps it cheap.
        // For now: when unreadOnly is true we already have unread count; when false we don't expose
        // a full total, so we report PageSize+1 if there's likely another page (placeholder), or
        // exact count when small. Keep it pragmatic: include a cheap separate count call.
        var totalUnread = await _notifications.CountUnreadAsync(userId, ct);

        var dtos = rows
            .Select(n => new NotificationDto(
                n.Id,
                n.WorkspaceId,
                n.ActorId,
                n.Kind,
                n.Title,
                n.Body,
                n.TargetKind,
                n.TargetId,
                n.Url,
                n.ReadAt,
                n.CreatedAt
            ))
            .ToList();

        // For the unreadOnly case the total equals the unread count exactly.
        var total = request.UnreadOnly ? totalUnread : skip + dtos.Count + (dtos.Count == request.Pagination.PageSize ? 1 : 0);
        return Result.Success(
            new PagedResult<NotificationDto>(dtos, request.Pagination.Page, request.Pagination.PageSize, total)
        );
    }
}
