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

    public Task<Workspace?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _ctx.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug.ToLower(), ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        _ctx.Workspaces.AnyAsync(w => w.Slug == slug.ToLower(), ct);

    public void Add(Workspace workspace) => _ctx.Workspaces.Add(workspace);

    public void Remove(Workspace workspace) => _ctx.Workspaces.Remove(workspace);
}
