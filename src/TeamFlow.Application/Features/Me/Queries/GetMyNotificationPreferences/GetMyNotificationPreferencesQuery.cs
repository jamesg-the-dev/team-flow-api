using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Me.DTOs;
using TeamFlow.Domain.Notifications;

namespace TeamFlow.Application.Features.Me.Queries.GetMyNotificationPreferences;

public sealed record GetMyNotificationPreferencesQuery(Guid? WorkspaceId = null)
    : IQuery<IReadOnlyList<MyNotificationPreferenceDto>>;

internal sealed class GetMyNotificationPreferencesHandler
    : IQueryHandler<GetMyNotificationPreferencesQuery, IReadOnlyList<MyNotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationPreferencesHandler(
        INotificationPreferenceRepository repository,
        ICurrentUser currentUser
    )
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MyNotificationPreferenceDto>>> Handle(
        GetMyNotificationPreferencesQuery request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        var prefs = request.WorkspaceId is { } ws
            ? await _repository.ListForUserAsync(userId, ws, ct)
            : await _repository.ListForUserAsync(userId, ct);

        IReadOnlyList<MyNotificationPreferenceDto> dtos = prefs
            .Select(p => new MyNotificationPreferenceDto(p.WorkspaceId, p.Kind, p.Channel, p.Enabled))
            .ToList();
        return Result.Success(dtos);
    }
}
