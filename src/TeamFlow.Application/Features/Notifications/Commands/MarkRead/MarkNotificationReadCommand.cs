using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Application.Features.Notifications.Commands.MarkRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;

internal sealed class MarkNotificationReadHandler : ICommandHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notifications;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkNotificationReadHandler(
        INotificationRepository notifications,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _notifications = notifications;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var n = await _notifications.GetByIdAsync(request.NotificationId, ct);
        if (n is null)
            return Error.NotFound("Notification not found.");
        if (n.RecipientId != _currentUser.RequireUserId())
            return Error.Forbidden("You can only mark your own notifications read.");
        n.MarkRead(_clock.UtcNow);
        return Result.Success();
    }
}
