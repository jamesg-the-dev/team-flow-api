using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.SeedWork;
using TeamFlow.Domain.Workspaces;

namespace TeamFlow.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceRepository : IWorkspaceRepository
{
    private readonly TeamFlowDbContext _ctx;

    public WorkspaceRepository(TeamFlowDbContext ctx, IUnitOfWork unitOfWork)
    {
        _ctx = ctx;
        UnitOfWork = unitOfWork;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Workspaces.Include(w => w.Members).FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<Workspace?> GetByIdWithInvitesAsync(Guid id, CancellationToken ct = default) =>
        _ctx
            .Workspaces.Include(w => w.Members)
            .Include(w => w.Invites)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<Workspace?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _ctx.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug.ToLower(), ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _ctx.Workspaces.AnyAsync(w => w.Slug == slug.ToLower(), ct);

    public async Task<IReadOnlyList<Guid>> ListIdsForUserAsync(
        Guid userId,
        CancellationToken ct = default
    ) =>
        await _ctx
            .WorkspaceMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

    public Task<bool> IsMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct = default
    ) =>
        _ctx.WorkspaceMembers.AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

    public Task<Workspace?> GetByInviteTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default
    ) =>
        _ctx
            .Workspaces.Include(w => w.Invites)
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Invites.Any(i => i.TokenHash == tokenHash), ct);

    public void Add(Workspace workspace) => _ctx.Workspaces.Add(workspace);

    public void Remove(Workspace workspace) => _ctx.Workspaces.Remove(workspace);
}
