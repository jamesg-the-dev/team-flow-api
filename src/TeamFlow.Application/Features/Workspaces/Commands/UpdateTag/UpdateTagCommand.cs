using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.UpdateTag;

public sealed record UpdateTagCommand(
    Guid WorkspaceId,
    Guid TagId,
    string? Name,
    string? ColorHex
) : ICommand;

public sealed class UpdateTagValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TagId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(64);
        RuleFor(x => x.ColorHex)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex))
            .WithMessage("Color must be a 7-char hex value (e.g. '#94A3B8').");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Name) || !string.IsNullOrWhiteSpace(x.ColorHex))
            .WithMessage("Provide at least one of 'name' or 'colorHex'.");
    }
}

internal sealed class UpdateTagHandler : ICommandHandler<UpdateTagCommand>
{
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public UpdateTagHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateTagCommand request, CancellationToken ct)
    {
        var workspace = await _workspaces.GetByIdWithTagsAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");
        if (!WorkspaceAuthorization.IsMember(workspace, _currentUser.RequireUserId()))
            return Error.Forbidden("You are not a member of this workspace.");

        try
        {
            workspace.UpdateTag(request.TagId, request.Name, request.ColorHex);
            return Result.Success();
        }
        catch (TeamFlow.Domain.SeedWork.DomainException ex)
            when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict(ex.Message);
        }
    }
}
