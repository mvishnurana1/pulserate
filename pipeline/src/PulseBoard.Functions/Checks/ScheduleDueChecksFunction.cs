using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PulseBoard.Monitoring.Application.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Functions.Checks;

/// <summary>
/// Every 15 seconds: claim the checks that are due and fan them out, one queue message per check.
/// Fan-out means one slow endpoint never delays the others, and the queue trigger scales out on its own.
/// </summary>
public sealed partial class ScheduleDueChecksFunction(
    ICommandHandler<ScheduleDueChecksCommand, Result<IReadOnlyList<Guid>>> handler,
    ILogger<ScheduleDueChecksFunction> logger)
{
    [Function("ScheduleDueChecks")]
    [QueueOutput(QueueNames.CheckRequests, Connection = QueueNames.Connection)]
    public async Task<string[]> Run(
        [TimerTrigger("*/15 * * * * *")] TimerInfo timer,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ScheduleDueChecksCommand(), cancellationToken);
        if (result.IsFailure || result.Value.Count == 0)
        {
            return [];
        }

        LogScheduled(result.Value.Count);
        return result.Value.Select(id => JsonSerializer.Serialize(new CheckRequest(id))).ToArray();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Queued {Count} due checks")]
    private partial void LogScheduled(int count);
}
