using Microsoft.EntityFrameworkCore;
using TeamFlow.Domain.Projects;
using TeamFlow.Domain.SeedWork;

namespace TeamFlow.Infrastructure.Persistence.Repositories;

internal sealed class ProjectRepository : IProjectRepository
{
    private readonly TeamFlowDbContext _ctx;

    public ProjectRepository(TeamFlowDbContext ctx, IUnitOfWork unitOfWork)
    {
        _ctx = ctx;
        UnitOfWork = unitOfWork;
    }

    public IUnitOfWork UnitOfWork { get; }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _ctx.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Project?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default) =>
        _ctx
            .Projects.Include(p => p.Members)
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> KeyExistsAsync(
        Guid workspaceId,
        string key,
        CancellationToken ct = default
    ) =>
        _ctx
            .Projects.IgnoreQueryFilters()
            .AnyAsync(p => p.WorkspaceId == workspaceId && p.Key == key && p.DeletedAt == null, ct);

    public void Add(Project project) => _ctx.Projects.Add(project);

    public void Remove(Project project) => _ctx.Projects.Remove(project);
}
