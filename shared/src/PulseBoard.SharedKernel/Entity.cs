namespace PulseBoard.SharedKernel;

/// <summary>
/// Lets infrastructure collect and clear domain events without knowing the entity's ID type.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

/// <summary>
/// Base type for entities that have an identity and can raise domain events.
/// </summary>
public abstract class Entity<TId> : IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TId Id { get; protected init; } = default!;

    /// <summary>
    /// Events raised by this entity since it was loaded. Written to the outbox in the same transaction as the change.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
