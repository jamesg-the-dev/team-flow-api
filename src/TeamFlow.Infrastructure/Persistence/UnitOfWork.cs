using TeamFlow.Domain.SeedWork;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TeamFlowDbContext _context;
    public UnitOfWork(TeamFlowDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
