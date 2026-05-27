using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Tasks.DTOs;
using TeamFlow.Application.Features.Tasks.Queries.GetProjectBoard;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

internal sealed class GetProjectBoardQueryService : IGetProjectBoardQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public GetProjectBoardQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<
        IReadOnlyDictionary<TaskColumn, IReadOnlyList<TaskBoardCardDto>>
    > ExecuteAsync(Guid projectId, CancellationToken ct)
    {
        var cards = await _ctx
            .Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.Column)
            .ThenBy(t => t.Position)
            .Select(t => new TaskBoardCardDto(
                t.Id,
                t.Number,
                t.Title,
                t.Column,
                t.Priority,
                t.Position,
                t.AssigneeId,
                t.DueDate
            ))
            .ToListAsync(ct);

        var grouped = Enum.GetValues<TaskColumn>()
            .ToDictionary(
                c => c,
                c => (IReadOnlyList<TaskBoardCardDto>)cards.Where(x => x.Column == c).ToList()
            );
        return grouped;
    }
}
