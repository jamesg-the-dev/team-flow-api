using FluentValidation;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Features.Projects.Commands.ChangeProjectStatus;

public sealed record ChangeProjectStatusCommand(Guid Id, ProjectStatus Status) : ICommand;

public sealed class ChangeProjectStatusValidator : AbstractValidator<ChangeProjectStatusCommand>
{
    public ChangeProjectStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

internal sealed class ChangeProjectStatusHandler : ICommandHandler<ChangeProjectStatusCommand>
{
    private readonly IProjectRepository _repository;

    public ChangeProjectStatusHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Result> Handle(ChangeProjectStatusCommand request, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(request.Id, ct);
        if (project is null)
            return Error.NotFound($"Project '{request.Id}' not found.");

        project.ChangeStatus(request.Status);
        return Result.Success();
    }
}
