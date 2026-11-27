using System.Text.Json;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Infrastructure.Outbox;

/// <summary>
/// An integration event waiting to be published. Written in the same transaction as the change that
/// caused it, so an event is never lost and never published for a change that rolled back.
/// </summary>
public sealed class OutboxMessage
{
    public const int ErrorMaxLength = 2000;

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private OutboxMessage()
    {
        // Required by EF Core.
    }

    /// <summary>Database identity: gives a reliable publish order.</summary>
    public long Id { get; private set; }

    /// <summary>Full name of the integration event type, resolved against the Contracts assembly.</summary>
    public string Type { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    internal static OutboxMessage From(IIntegrationEvent integrationEvent) => new()
    {
        Type = integrationEvent.GetType().FullName!,
        Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions),
        OccurredAt = integrationEvent.OccurredAt,
    };

    internal void MarkProcessed(DateTimeOffset processedAt) => ProcessedAt = processedAt;

    /// <summary>Records a failed attempt. After <paramref name="maxAttempts"/> the message is parked so it stops blocking.</summary>
    internal void MarkFailed(string error, int maxAttempts, DateTimeOffset failedAt)
    {
        Attempts++;
        LastError = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;

        if (Attempts >= maxAttempts)
        {
            ProcessedAt = failedAt;
        }
    }
}
