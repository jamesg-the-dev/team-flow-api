using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Notifications.DTOs;
using TeamFlow.Domain.Notifications;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Notifications.Queries.ListPreferences;

public sealed record ListPreferencesQuery(Guid WorkspaceId)
    : IQuery<IReadOnlyList<NotificationPreferenceDto>>;

internal sealed class ListPreferencesHandler
    : IQueryHandler<ListPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _prefs;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public ListPreferencesHandler(
        INotificationPreferenceRepository prefs,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _prefs = prefs;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(
        ListPreferencesQuery request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");

        var prefs = await _prefs.ListForUserAsync(userId, request.WorkspaceId, ct);
        var dtos = prefs
            .Select(p => new NotificationPreferenceDto(p.Kind, p.Channel, p.Enabled))
            .ToList();
        return Result.Success<IReadOnlyList<NotificationPreferenceDto>>(dtos);
    }
}
