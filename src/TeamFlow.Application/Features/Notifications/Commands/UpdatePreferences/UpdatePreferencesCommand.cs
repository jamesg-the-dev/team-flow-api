using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Notifications;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Notifications.Commands.UpdatePreferences;

/// <summary>
/// Replaces the current user's notification preferences for the given workspace with the supplied
/// (kind, channel, enabled) triples. Items missing from the request are removed, treating
/// "no preference" as default-on per the dispatcher policy.
/// </summary>
public sealed record UpdatePreferencesCommand(
    Guid WorkspaceId,
    IReadOnlyList<PreferenceItem> Preferences
) : ICommand;

public sealed record PreferenceItem(NotificationKind Kind, DeliveryChannel Channel, bool Enabled);

public sealed class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Preferences).NotNull();
        RuleForEach(x => x.Preferences)
            .ChildRules(p =>
            {
                p.RuleFor(i => i.Kind).IsInEnum();
                p.RuleFor(i => i.Channel).IsInEnum();
            });
    }
}

internal sealed class UpdatePreferencesHandler : ICommandHandler<UpdatePreferencesCommand>
{
    private readonly INotificationPreferenceRepository _prefs;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public UpdatePreferencesHandler(
        INotificationPreferenceRepository prefs,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _prefs = prefs;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");

        var existing = await _prefs.ListForUserAsync(userId, request.WorkspaceId, ct);
        var incoming = request.Preferences
            .GroupBy(p => (p.Kind, p.Channel))
            .ToDictionary(g => g.Key, g => g.Last().Enabled);

        // Update or remove existing rows.
        foreach (var pref in existing)
        {
            if (incoming.TryGetValue((pref.Kind, pref.Channel), out var enabled))
            {
                if (enabled) pref.Enable();
                else pref.Disable();
                incoming.Remove((pref.Kind, pref.Channel));
            }
            else
            {
                _prefs.Remove(pref);
            }
        }

        // Insert anything left over.
        foreach (var ((kind, channel), enabled) in incoming)
        {
            _prefs.Add(
                new NotificationPreference(userId, request.WorkspaceId, kind, channel, enabled)
            );
        }
        return Result.Success();
    }
}
