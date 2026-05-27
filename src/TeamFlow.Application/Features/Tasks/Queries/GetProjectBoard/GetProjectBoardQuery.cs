using TeamFlow.Application.Common.Messaging;
using TeamFlow.Application.Common.Results;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Tasks.Queries.GetProjectBoard;

public sealed record GetProjectBoardQuery(Guid ProjectId)
    : IQuery<IReadOnlyDictionary<TaskColumn, IReadOnlyList<TaskBoardCardDto>>>;

/// <summary>Optimised read-side query (projection, no aggregate hydration). Implemented in Infrastructure.</summary>
public interface IGetProjectBoardQueryService
{
    Task<IReadOnlyDictionary<TaskColumn, IReadOnlyList<TaskBoardCardDto>>> ExecuteAsync(
        Guid projectId,
        CancellationToken ct
    );
}

internal sealed class GetProjectBoardHandler
    : IQueryHandler<
        GetProjectBoardQuery,
        IReadOnlyDictionary<TaskColumn, IReadOnlyList<TaskBoardCardDto>>
    >
{
    private readonly IGetProjectBoardQueryService _service;

    public GetProjectBoardHandler(IGetProjectBoardQueryService service) => _service = service;

    public async Task<
        Result<IReadOnlyDictionary<TaskColumn, IReadOnlyList<TaskBoardCardDto>>>
    > Handle(GetProjectBoardQuery request, CancellationToken ct)
    {
        var board = await _service.ExecuteAsync(request.ProjectId, ct);
        return Result.Success(board);
    }
}
