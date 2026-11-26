namespace PulseBoard.SharedKernel;

/// <summary>
/// Base type for entities that have an identity and can raise domain events.
/// </summary>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected init; } = default!;

    /// <summary>
    /// Events raised by this entity since it was loaded. Dispatched after the unit of work commits.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
