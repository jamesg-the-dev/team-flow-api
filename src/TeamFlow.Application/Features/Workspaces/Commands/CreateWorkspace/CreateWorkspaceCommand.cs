using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(string Slug, string Name, string? LogoUrl)
    : ICommand<WorkspaceDto>;

public sealed class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches("^[a-z0-9](?:[a-z0-9-]{1,38}[a-z0-9])?$")
            .WithMessage("Slug must be 2-40 lowercase alphanumerics/dashes, no leading/trailing dash.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LogoUrl).MaximumLength(2000);
    }
}

internal sealed class CreateWorkspaceHandler : ICommandHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    private readonly IWorkspaceRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateWorkspaceHandler(IWorkspaceRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDto>> Handle(
        CreateWorkspaceCommand request,
        CancellationToken ct
    )
    {
        if (await _repository.SlugExistsAsync(request.Slug, ct))
            return Error.Conflict($"Workspace slug '{request.Slug}' is already taken.");

        var workspace = Workspace.Create(request.Slug, request.Name, _currentUser.RequireUserId());
        if (!string.IsNullOrWhiteSpace(request.LogoUrl))
            workspace.SetLogo(request.LogoUrl);

        _repository.Add(workspace);

        return new WorkspaceDto(
            workspace.Id,
            workspace.Slug,
            workspace.Name,
            workspace.LogoUrl,
            workspace.Plan,
            workspace.OwnerId,
            workspace.Members.Count,
            workspace.CreatedAt,
            workspace.UpdatedAt
        );
    }
}
