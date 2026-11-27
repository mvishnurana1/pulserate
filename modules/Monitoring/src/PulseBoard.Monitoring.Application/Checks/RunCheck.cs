using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

/// <summary>Calls the check's endpoint once and records the outcome.</summary>
public sealed record RunCheckCommand(Guid CheckId);

internal sealed class RunCheckHandler(IMonitoringDbContext db, IEndpointProber prober, TimeProvider clock)
    : ICommandHandler<RunCheckCommand, Result<CheckResultResponse>>
{
    public async Task<Result<CheckResultResponse>> HandleAsync(RunCheckCommand command, CancellationToken cancellationToken)
    {
        var check = await db.Checks.FirstOrDefaultAsync(c => c.Id == command.CheckId, cancellationToken);
        if (check is null)
        {
            // Deleted after it was queued.
            return CheckErrors.NotFound(command.CheckId);
        }

        if (check.IsPaused)
        {
            return CheckErrors.IsPaused;
        }

        var outcome = await prober.ProbeAsync(check.Url, cancellationToken);

        var recorded = check.RecordResult(outcome, clock.GetUtcNow());
        if (recorded.IsFailure)
        {
            return recorded.Error;
        }

        db.CheckResults.Add(recorded.Value);

        // Saves the result, the check's new status and its domain events (as outbox messages) in one transaction.
        await db.SaveChangesAsync(cancellationToken);

        return CheckResultResponse.From(recorded.Value);
    }
}
