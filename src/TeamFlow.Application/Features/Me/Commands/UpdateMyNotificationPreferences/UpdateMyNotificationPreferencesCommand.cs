using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Notifications;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Me.Commands.UpdateMyNotificationPreferences;

public sealed record NotificationPreferenceInput(
    NotificationKind Kind,
    DeliveryChannel Channel,
    bool Enabled
);

/// <summary>
/// Replaces the caller's notification preferences for a single workspace. Any rows missing
/// from <see cref="Items"/> are removed; matching rows are inserted or updated.
/// </summary>
public sealed record UpdateMyNotificationPreferencesCommand(
    Guid WorkspaceId,
    IReadOnlyList<NotificationPreferenceInput> Items
) : ICommand;

public sealed class UpdateMyNotificationPreferencesValidator
    : AbstractValidator<UpdateMyNotificationPreferencesCommand>
{
    public UpdateMyNotificationPreferencesValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items)
            .Must(i => Enum.IsDefined(i.Kind) && Enum.IsDefined(i.Channel))
            .WithMessage("Unknown notification kind or delivery channel.");
    }
}

internal sealed class UpdateMyNotificationPreferencesHandler
    : ICommandHandler<UpdateMyNotificationPreferencesCommand>
{
    private readonly INotificationPreferenceRepository _preferences;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public UpdateMyNotificationPreferencesHandler(
        INotificationPreferenceRepository preferences,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _preferences = preferences;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateMyNotificationPreferencesCommand request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();

        var membershipIds = await _workspaces.ListIdsForUserAsync(userId, ct);
        if (!membershipIds.Contains(request.WorkspaceId))
            return Error.Forbidden("You are not a member of this workspace.");

        var current = await _preferences.ListForUserAsync(userId, request.WorkspaceId, ct);

        // Diff existing against incoming using the composite natural key.
        var incomingByKey = request
            .Items.GroupBy(i => (i.Kind, i.Channel))
            .ToDictionary(g => g.Key, g => g.Last());

        // Remove rows the caller dropped from the set.
        var toRemove = current
            .Where(p => !incomingByKey.ContainsKey((p.Kind, p.Channel)))
            .ToList();
        if (toRemove.Count > 0)
            _preferences.RemoveRange(toRemove);

        // Upsert remaining rows.
        var currentByKey = current.ToDictionary(p => (p.Kind, p.Channel));
        foreach (var (key, item) in incomingByKey)
        {
            if (currentByKey.TryGetValue(key, out var row))
            {
                if (row.Enabled != item.Enabled)
                {
                    if (item.Enabled)
                        row.Enable();
                    else
                        row.Disable();
                }
            }
            else
            {
                _preferences.Add(
                    new NotificationPreference(
                        userId,
                        request.WorkspaceId,
                        item.Kind,
                        item.Channel,
                        item.Enabled
                    )
                );
            }
        }

        return Result.Success();
    }
}
