using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PulseBoard.Monitoring.Contracts;
using PulseBoard.Monitoring.Infrastructure.Persistence;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Infrastructure.Outbox;

/// <summary>
/// Publishes outbox messages to in-process integration event handlers (the Incidents module and the
/// API's SignalR notifier). Safe to run on several API instances: rows are claimed with
/// <c>FOR UPDATE SKIP LOCKED</c>, so each message is picked up by one instance at a time.
/// Delivery is at-least-once.
/// </summary>
internal sealed partial class MonitoringOutboxProcessor(
    IServiceScopeFactory scopeFactory,
    TimeProvider clock,
    ILogger<MonitoringOutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, clock);

        do
        {
            try
            {
                // Keep draining while full batches come back, then wait for the next tick.
                while (await ProcessBatchAsync(stoppingToken) == BatchSize)
                {
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogBatchFailed(ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();

        // Aspire turns on connection retries, which require transactions to run inside the execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async ct =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var messages = await db.OutboxMessages
                .FromSql($"""
                    SELECT * FROM monitoring."OutboxMessages"
                    WHERE "ProcessedAt" IS NULL
                    ORDER BY "Id"
                    LIMIT {BatchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(ct);

            foreach (var message in messages)
            {
                try
                {
                    await dispatcher.DispatchAsync(Deserialize(message), ct);
                    message.MarkProcessed(clock.GetUtcNow());
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogMessageFailed(ex, message.Id, message.Type, message.Attempts + 1);
                    message.MarkFailed(ex.Message, MaxAttempts, clock.GetUtcNow());
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return messages.Count;
        }, cancellationToken);
    }

    private static IIntegrationEvent Deserialize(OutboxMessage message)
    {
        // Only types from the Contracts assembly can be loaded, never arbitrary type names from the database.
        var type = typeof(CheckWentDownIntegrationEvent).Assembly.GetType(message.Type)
            ?? throw new InvalidOperationException($"Unknown integration event type '{message.Type}'.");

        return (IIntegrationEvent)(JsonSerializer.Deserialize(message.Content, type, OutboxMessage.JsonOptions)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has empty content."));
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox batch failed; retrying on the next tick")]
    private partial void LogBatchFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({Type}) failed on attempt {Attempt}")]
    private partial void LogMessageFailed(Exception exception, long messageId, string type, int attempt);
}
