namespace PulseBoard.SharedKernel;

/// <summary>
/// Something meaningful that happened inside a module's domain.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
