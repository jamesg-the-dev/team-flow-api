namespace TeamFlow.Domain.SeedWork;

/// <summary>Base entity using a Guid surrogate key (UUID v4/v7 from PostgreSQL).</summary>
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    protected Entity() { }
    protected Entity(Guid id) => Id = id;

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id && Id != Guid.Empty;

    public override bool Equals(object? obj) => Equals(obj as Entity);
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
    public static bool operator ==(Entity? a, Entity? b) => a?.Equals(b) ?? b is null;
    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}

/// <summary>Aggregate root base. Aggregates are the only entry point for state mutation.</summary>
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>Aggregate root that also carries audit metadata.</summary>
public abstract class AuditableAggregateRoot : AggregateRoot, IAuditable
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    void IAuditable.SetCreated(DateTimeOffset at, Guid? by)
    {
        CreatedAt = at;
        UpdatedAt = at;
        CreatedBy = by;
        UpdatedBy = by;
    }

    void IAuditable.SetUpdated(DateTimeOffset at, Guid? by)
    {
        UpdatedAt = at;
        UpdatedBy = by;
    }

    protected AuditableAggregateRoot() { }
    protected AuditableAggregateRoot(Guid id) : base(id) { }
}

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
    Guid? CreatedBy { get; }
    Guid? UpdatedBy { get; }
    void SetCreated(DateTimeOffset at, Guid? by);
    void SetUpdated(DateTimeOffset at, Guid? by);
}

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
    void SoftDelete(DateTimeOffset at);
}
