using PulseBoard.SharedKernel;

namespace PulseBoard.Monitoring.Domain.Checks;

/// <summary>Raised every time a result is recorded. Drives the live dashboard.</summary>
public sealed record CheckResultRecorded(
    Guid CheckId,
    Guid ResultId,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? Error,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a check crosses its failure threshold.</summary>
public sealed record CheckWentDown(
    Guid CheckId,
    int ConsecutiveFailures,
    string? LastError,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised on the first successful check after being down.</summary>
public sealed record CheckRecovered(Guid CheckId, DateTimeOffset OccurredAt) : IDomainEvent;
