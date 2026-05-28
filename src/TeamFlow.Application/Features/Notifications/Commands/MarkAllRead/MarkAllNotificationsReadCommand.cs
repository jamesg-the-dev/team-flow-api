using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Application.Features.Notifications.Commands.MarkAllRead;

/// <summary>Bulk-marks every unread notification for the current user as read.</summary>
public sealed record MarkAllNotificationsReadCommand() : ICommand<int>;

internal sealed class MarkAllNotificationsReadHandler
    : ICommandHandler<MarkAllNotificationsReadCommand, int>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkAllNotificationsReadHandler(
        INotificationRepository notifications,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<int>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken ct
    )
    {
        var count = await _notifications.MarkAllReadAsync(
            _currentUser.RequireUserId(),
            _clock.UtcNow,
            ct
        );
        return Result.Success(count);
    }
}
