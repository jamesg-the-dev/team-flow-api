using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Common.Security;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Invites.Commands.AcceptInvite;

/// <summary>
/// Accepts a workspace invite for the currently-authenticated user. The token is the plain
/// value previously returned by <c>POST /workspaces/{id}/invites</c>.
/// </summary>
public sealed record AcceptInviteCommand(string Token) : ICommand<AcceptInviteResultDto>;

public sealed class AcceptInviteValidator : AbstractValidator<AcceptInviteCommand>
{
    public AcceptInviteValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(256);
    }
}

internal sealed class AcceptInviteHandler
    : ICommandHandler<AcceptInviteCommand, AcceptInviteResultDto>
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AcceptInviteHandler(
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<AcceptInviteResultDto>> Handle(
        AcceptInviteCommand request,
        CancellationToken ct
    )
    {
        var email = _currentUser.Email;
        if (string.IsNullOrWhiteSpace(email))
            return Error.Forbidden("Your account has no email address on file.");
        if (!_currentUser.EmailVerified)
            return Error.Forbidden("You must verify your email before accepting invites.");

        var hash = InviteToken.Hash(request.Token);
        var workspace = await _workspaces.GetByInviteTokenHashAsync(hash, ct);
        if (workspace is null)
            return Error.NotFound("Invite not found or already revoked.");

        var invite = workspace.Invites.First(i => i.TokenHash == hash);
        if (invite.AcceptedAt is not null)
            return Error.Conflict("This invite has already been accepted.");
        if (invite.ExpiresAt <= _clock.UtcNow)
            return Error.Validation("This invite has expired.", "invite.expired");

        var userId = _currentUser.RequireUserId();
        var created = workspace.AcceptInvite(invite.Id, userId, email!, _clock.UtcNow);

        return new AcceptInviteResultDto(
            workspace.Id,
            workspace.Slug,
            workspace.Name,
            invite.Role,
            created
        );
    }
}
