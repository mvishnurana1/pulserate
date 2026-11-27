using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PulseBoard.Monitoring.Application.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Functions.Checks;

/// <summary>
/// Runs one check per queue message. Expected outcomes (check deleted or paused since it was queued) are
/// logged and the message completes. Unexpected exceptions (database down, edit conflict) are thrown, so the
/// queue retries the message and parks it in the poison queue after five attempts.
/// </summary>
public sealed partial class RunCheckFunction(
    ICommandHandler<RunCheckCommand, Result<CheckResultResponse>> handler,
    ILogger<RunCheckFunction> logger)
{
    [Function("RunCheck")]
    public async Task Run(
        [QueueTrigger(QueueNames.CheckRequests, Connection = QueueNames.Connection)] CheckRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RunCheckCommand(request.CheckId), cancellationToken);

        if (result.IsFailure)
        {
            LogSkipped(request.CheckId, result.Error.Code);
            return;
        }

        LogCompleted(request.CheckId, result.Value.IsSuccess, result.Value.ResponseTimeMs);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped check {CheckId}: {Reason}")]
    private partial void LogSkipped(Guid checkId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Check {CheckId} finished: success={IsSuccess} in {ResponseTimeMs} ms")]
    private partial void LogCompleted(Guid checkId, bool isSuccess, int responseTimeMs);
}
