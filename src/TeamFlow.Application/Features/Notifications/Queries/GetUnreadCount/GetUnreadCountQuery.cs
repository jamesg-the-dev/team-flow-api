using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Application.Features.Notifications.Queries.GetUnreadCount;

/// <summary>Returns the current user's unread-notification badge count.</summary>
public sealed record GetUnreadCountQuery() : IQuery<int>;

internal sealed class GetUnreadCountHandler : IQueryHandler<GetUnreadCountQuery, int>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUser _currentUser;

    public GetUnreadCountHandler(INotificationRepository notifications, ICurrentUser currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken ct) =>
        await _notifications.CountUnreadAsync(_currentUser.RequireUserId(), ct);
}
