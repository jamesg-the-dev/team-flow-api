using TeamFlow.Application.Common.Abstractions;
using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Application.Features.Dashboard.Queries.GetWorkspaceDashboard;

/// <summary>Single-call snapshot the home page renders from.</summary>
public sealed record WorkspaceDashboardDto(
    Guid WorkspaceId,
    int OpenTasksAssignedToMe,
    int OverdueTasksAssignedToMe,
    int DueSoonTasksAssignedToMe,
    int UnreadNotifications,
    int UnreadChannels,
    int MyProjectsCount
);

public sealed record GetWorkspaceDashboardQuery(Guid WorkspaceId) : IQuery<WorkspaceDashboardDto>;

public interface IGetWorkspaceDashboardQueryService
{
    Task<WorkspaceDashboardDto> ExecuteAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct
    );
}

internal sealed class GetWorkspaceDashboardHandler
    : IQueryHandler<GetWorkspaceDashboardQuery, WorkspaceDashboardDto>
{
    private readonly IGetWorkspaceDashboardQueryService _svc;
    private readonly IWorkspaceRepository _workspaces;
    private readonly ICurrentUser _currentUser;

    public GetWorkspaceDashboardHandler(
        IGetWorkspaceDashboardQueryService svc,
        IWorkspaceRepository workspaces,
        ICurrentUser currentUser
    )
    {
        _svc = svc;
        _workspaces = workspaces;
        _currentUser = currentUser;
    }

    public async Task<Result<WorkspaceDashboardDto>> Handle(
        GetWorkspaceDashboardQuery request,
        CancellationToken ct
    )
    {
        var userId = _currentUser.RequireUserId();
        if (!await _workspaces.IsMemberAsync(request.WorkspaceId, userId, ct))
            return Error.Forbidden("You are not a member of this workspace.");

        return await _svc.ExecuteAsync(request.WorkspaceId, userId, ct);
    }
}
