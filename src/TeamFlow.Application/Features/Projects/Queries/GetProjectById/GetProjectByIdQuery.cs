using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Projects.DTOs;
using TeamFlow.Domain.Projects;

namespace TeamFlow.Application.Features.Projects.Queries.GetProjectById;

public sealed record GetProjectByIdQuery(Guid Id) : IQuery<ProjectDto>;

internal sealed class GetProjectByIdHandler : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _repository;
    public GetProjectByIdHandler(IProjectRepository repository) => _repository = repository;

    public async Task<Result<ProjectDto>> Handle(GetProjectByIdQuery request, CancellationToken ct)
    {
        var p = await _repository.GetByIdAsync(request.Id, ct);
        if (p is null) return Error.NotFound($"Project '{request.Id}' not found.");

        return new ProjectDto(
            p.Id, p.WorkspaceId, p.Key, p.Name, p.Description, p.Status, p.Priority,
            p.StartDate, p.DueDate, p.Budget?.AmountCents, p.Budget?.Currency, p.ColorHex,
            p.CreatedAt, p.UpdatedAt);
    }
}
