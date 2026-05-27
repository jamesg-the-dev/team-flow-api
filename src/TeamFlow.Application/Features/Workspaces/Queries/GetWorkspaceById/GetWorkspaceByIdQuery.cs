using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Workspaces.DTOs;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Workspaces.Queries.GetWorkspaceById;

public sealed record GetWorkspaceByIdQuery(Guid WorkspaceId) : IQuery<WorkspaceDto>;

internal sealed class GetWorkspaceByIdHandler : IQueryHandler<GetWorkspaceByIdQuery, WorkspaceDto>
{
    private readonly IWorkspaceRepository _repository;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceByIdHandler(IWorkspaceRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDto>> Handle(
        GetWorkspaceByIdQuery request,
        CancellationToken ct
    )
    {
        var workspace = await _repository.GetByIdAsync(request.WorkspaceId, ct);
        if (workspace is null)
            return Error.NotFound("Workspace not found.");

        var userId = _currentUser.RequireUserId();
        if (workspace.Members.All(m => m.UserId != userId))
            return Error.Forbidden("You are not a member of this workspace.");

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
