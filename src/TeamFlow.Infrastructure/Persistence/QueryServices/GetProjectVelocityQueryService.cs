using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Projects.Queries.GetProjectVelocity;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Infrastructure.Persistence.QueryServices;

/// <summary>
/// Computes per-ISO-week task creation / completion counts for a project. Weeks are anchored on
/// Monday in UTC. The result always has exactly <c>weeks</c> rows (the most recent window),
/// filling in zero-rows for weeks with no activity so the UI can render a continuous chart.
/// </summary>
internal sealed class GetProjectVelocityQueryService : IGetProjectVelocityQueryService
{
    private readonly TeamFlowDbContext _ctx;

    public GetProjectVelocityQueryService(TeamFlowDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<VelocityPointDto>> ExecuteAsync(
        Guid projectId,
        int weeks,
        CancellationToken ct
    )
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thisMonday = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1));
        var firstMonday = thisMonday.AddDays(-7 * (weeks - 1));
        var rangeStart = new DateTimeOffset(firstMonday.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        // Pull just the timestamps and bucket in memory. The trailing window is bounded
        // (52 weeks max) and projects rarely produce enough tasks to make this expensive;
        // doing it in app code avoids depending on Postgres `date_trunc` provider support.
        var createdStamps = await _ctx
            .Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId && t.CreatedAt >= rangeStart)
            .Select(t => t.CreatedAt)
            .ToListAsync(ct);

        var completedStamps = await _ctx
            .Tasks.AsNoTracking()
            .Where(t =>
                t.ProjectId == projectId
                && t.CompletedAt != null
                && t.CompletedAt >= rangeStart
                && t.Column == TaskColumn.Done
            )
            .Select(t => t.CompletedAt!.Value)
            .ToListAsync(ct);

        var createdMap = BucketByMonday(createdStamps);
        var completedMap = BucketByMonday(completedStamps);

        var result = new List<VelocityPointDto>(weeks);
        for (var i = 0; i < weeks; i++)
        {
            var ws = firstMonday.AddDays(7 * i);
            result.Add(
                new VelocityPointDto(
                    ws,
                    completedMap.GetValueOrDefault(ws),
                    createdMap.GetValueOrDefault(ws)
                )
            );
        }
        return result;
    }

    private static Dictionary<DateOnly, int> BucketByMonday(IEnumerable<DateTimeOffset> stamps)
    {
        var result = new Dictionary<DateOnly, int>();
        foreach (var ts in stamps)
        {
            var d = DateOnly.FromDateTime(ts.UtcDateTime);
            var weekday = (int)d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1;
            var monday = d.AddDays(-weekday);
            result[monday] = result.GetValueOrDefault(monday) + 1;
        }
        return result;
    }
}
