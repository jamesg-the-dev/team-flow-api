using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Common.Security;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.CreateInvite;

/// <summary>
/// Creates a single-use invite. Default expiry is 7 days. The plain-text token is returned
/// once in <see cref="CreatedInviteDto.Token"/> — only its SHA-256 hash is persisted.
/// </summary>
public sealed record CreateInviteCommand(
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role,
    TimeSpan? Expiry = null
) : ICommand<CreatedInviteDto>;

public sealed class CreateInviteValidator : AbstractValidator<CreateInviteCommand>
{
    public CreateInviteValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Expiry)
            .Must(e => e is null || (e > TimeSpan.Zero && e <= TimeSpan.FromDays(60)))
            .WithMessage("Expiry must be between 0 and 60 days.");
    }
}

internal sealed class CreateInviteHandler : ICommandHandler<CreateInviteCommand, CreatedInviteDto>
{
    public static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateInviteHandler(
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser,
        IDateTimeProvider clock
    )
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<CreatedInviteDto>> Handle(
        CreateInviteCommand request,
        CancellationToken ct
    )
    {
        var workspace = await _workspaces.GetByIdWithInvitesAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        var callerId = _currentUser.RequireUserId();
        if (!WorkspaceAuthorization.IsOwnerOrAdmin(workspace, callerId))
            return Error.Forbidden("Only workspace owners or admins can issue invites.");

        var (plain, hash) = InviteToken.Generate();
        var expiresAt = _clock.UtcNow + (request.Expiry ?? DefaultExpiry);

        var invite = workspace.InviteUser(request.Email, request.Role, hash, callerId, expiresAt);

        var dto = new WorkspaceInviteDto(
            invite.Id,
            invite.Email,
            invite.Role,
            invite.InvitedBy,
            invite.ExpiresAt,
            invite.AcceptedAt,
            invite.CreatedAt
        );
        return new CreatedInviteDto(dto, plain);
    }
}
