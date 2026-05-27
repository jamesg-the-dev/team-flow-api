using FluentValidation;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description,
    PriorityLevel Priority,
    DateOnly? StartDate,
    DateOnly? DueDate,
    string? ColorHex) : ICommand;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.ColorHex).Matches("^#[0-9A-Fa-f]{6}$").When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
    }
}

internal sealed class UpdateProjectHandler : ICommandHandler<UpdateProjectCommand>
{
    private readonly IProjectRepository _repository;
    public UpdateProjectHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Result> Handle(UpdateProjectCommand request, CancellationToken ct)
    {
        var project = await _repository.GetByIdAsync(request.Id, ct);
        if (project is null) return Error.NotFound($"Project '{request.Id}' not found.");

        project.UpdateDetails(request.Name, request.Description, request.Priority,
            request.StartDate, request.DueDate, request.ColorHex);
        return Result.Success();
    }
}
