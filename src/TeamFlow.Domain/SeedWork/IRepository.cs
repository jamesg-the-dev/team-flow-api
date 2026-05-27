namespace TeamFlow.Domain.SeedWork;

/// <summary>
/// Repository interface marker. The aggregate root is the consistency boundary;
/// there is exactly one repository per aggregate root.
/// </summary>
public interface IRepository<T> where T : class, IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
