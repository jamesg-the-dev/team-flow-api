using FluentValidation;
using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    Guid WorkspaceId,
    string Key,
    string Name,
    string? Description,
    PriorityLevel Priority,
    DateOnly? StartDate,
    DateOnly? DueDate,
    string? ColorHex) : ICommand<ProjectDto>;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().Matches("^[A-Z][A-Z0-9]{1,9}$")
            .WithMessage("Key must be 2-10 uppercase alphanumerics starting with a letter.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.ColorHex).Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
        RuleFor(x => x).Must(c => c.StartDate is null || c.DueDate is null || c.DueDate >= c.StartDate)
            .WithMessage("Due date cannot precede start date.");
    }
}

internal sealed class CreateProjectHandler : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateProjectHandler(IProjectRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken ct)
    {
        if (await _repository.KeyExistsAsync(request.WorkspaceId, request.Key, ct))
            return Error.Conflict($"Project key '{request.Key}' already exists in this workspace.");

        var userId = _currentUser.RequireUserId();
        var project = Project.Create(
            request.WorkspaceId,
            request.Key,
            request.Name,
            createdBy: userId,
            description: request.Description,
            priority: request.Priority,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            colorHex: request.ColorHex);

        _repository.Add(project);

        return new ProjectDto(
            project.Id, project.WorkspaceId, project.Key, project.Name, project.Description,
            project.Status, project.Priority, project.StartDate, project.DueDate,
            project.Budget?.AmountCents, project.Budget?.Currency, project.ColorHex,
            project.CreatedAt, project.UpdatedAt);
    }
}
