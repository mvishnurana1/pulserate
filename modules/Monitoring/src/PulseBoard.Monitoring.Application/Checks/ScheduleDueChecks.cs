using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

/// <summary>Finds checks that are due and claims them. The caller queues one run per returned ID.</summary>
public sealed record ScheduleDueChecksCommand(int BatchSize = 200);

internal sealed class ScheduleDueChecksHandler(IMonitoringDbContext db, TimeProvider clock)
    : ICommandHandler<ScheduleDueChecksCommand, Result<IReadOnlyList<Guid>>>
{
    public async Task<Result<IReadOnlyList<Guid>>> HandleAsync(
        ScheduleDueChecksCommand command,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        var due = await db.Checks
            .Where(c => c.Status != CheckStatus.Paused && c.NextCheckAt <= now)
            .OrderBy(c => c.NextCheckAt)
            .Take(command.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var check in due)
        {
            check.MarkScheduled(now);
        }

        // If two scheduler instances race, the xmin concurrency token makes one of them fail
        // here instead of both queueing the same checks.
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success<IReadOnlyList<Guid>>(due.Select(c => c.Id).ToList());
    }
}
