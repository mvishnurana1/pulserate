using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Contracts;

// The Monitoring module's public events. Other modules (and the API's real-time layer) depend on these,
// never on Monitoring's domain types, so Monitoring can change internally without breaking them.

/// <summary>A check ran and its result was saved.</summary>
public sealed record CheckResultRecordedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CheckId,
    Guid ResultId,
    string CheckName,
    string Status,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? Error,
    int ConsecutiveFailures) : IIntegrationEvent;

/// <summary>A check failed enough times in a row to count as down.</summary>
public sealed record CheckWentDownIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CheckId,
    string CheckName,
    string Url,
    int ConsecutiveFailures,
    string? LastError) : IIntegrationEvent;

/// <summary>A check that was down succeeded again.</summary>
public sealed record CheckRecoveredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid CheckId,
    string CheckName) : IIntegrationEvent;
