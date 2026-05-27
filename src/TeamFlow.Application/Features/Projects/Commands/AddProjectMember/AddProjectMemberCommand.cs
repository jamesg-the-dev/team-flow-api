using FluentValidation;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Features.Projects.Commands.AddProjectMember;

public sealed record AddProjectMemberCommand(Guid ProjectId, Guid UserId, ProjectMemberRole Role) : ICommand;

public sealed class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

internal sealed class AddProjectMemberHandler : ICommandHandler<AddProjectMemberCommand>
{
    private readonly IProjectRepository _repository;
    public AddProjectMemberHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Result> Handle(AddProjectMemberCommand request, CancellationToken ct)
    {
        var project = await _repository.GetByIdWithMembersAsync(request.ProjectId, ct);
        if (project is null) return Error.NotFound($"Project '{request.ProjectId}' not found.");
        project.AddMember(request.UserId, request.Role);
        return Result.Success();
    }
}
