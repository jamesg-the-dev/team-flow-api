using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.CreateTag;

public sealed record CreateTagCommand(Guid WorkspaceId, string Name, string? ColorHex)
    : ICommand<TagDto>;

public sealed class CreateTagValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ColorHex)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex))
            .WithMessage("Color must be a 7-char hex value (e.g. '#94A3B8').");
    }
}

internal sealed class CreateTagHandler : ICommandHandler<CreateTagCommand, TagDto>
{
    private const string DefaultColor = "#94A3B8";

    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public CreateTagHandler(IWorkspaceRepository workspaces, ICurrentUser currentUser)
    {
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<TagDto>> Handle(CreateTagCommand request, CancellationToken ct)
    {
        var workspace = await _workspaces.GetByIdWithTagsAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");
        if (!WorkspaceAuthorization.IsMember(workspace, _currentUser.RequireUserId()))
            return Error.Forbidden("You are not a member of this workspace.");

        try
        {
            var tag = workspace.CreateTag(request.Name, request.ColorHex ?? DefaultColor);
            return new TagDto(tag.Id, tag.WorkspaceId, tag.Name, tag.ColorHex);
        }
        catch (TeamFlow.Domain.SeedWork.DomainException ex)
            when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict(ex.Message);
        }
    }
}
