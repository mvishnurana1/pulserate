using PulseBoard.Monitoring.Contracts;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Infrastructure.Outbox;

/// <summary>
/// Translates internal domain events into the module's public integration events,
/// adding the details other modules need (such as the check's name) so they don't have to call back.
/// </summary>
internal static class IntegrationEventMapper
{
    public static IIntegrationEvent Map(Check check, IDomainEvent domainEvent) => domainEvent switch
    {
        CheckResultRecorded e => new CheckResultRecordedIntegrationEvent(
            Guid.CreateVersion7(),
            e.OccurredAt,
            check.Id,
            e.ResultId,
            check.Name,
            check.Status.ToString(),
            e.IsSuccess,
            e.StatusCode,
            e.ResponseTimeMs,
            e.Error,
            check.ConsecutiveFailures),

        CheckWentDown e => new CheckWentDownIntegrationEvent(
            Guid.CreateVersion7(),
            e.OccurredAt,
            check.Id,
            check.Name,
            check.Url.AbsoluteUri,
            e.ConsecutiveFailures,
            e.LastError),

        CheckRecovered e => new CheckRecoveredIntegrationEvent(
            Guid.CreateVersion7(),
            e.OccurredAt,
            check.Id,
            check.Name),

        _ => throw new InvalidOperationException($"No integration event mapping for {domainEvent.GetType().Name}."),
    };
}
