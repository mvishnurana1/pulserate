using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

public sealed record PauseCheckCommand(Guid Id);

public sealed record ResumeCheckCommand(Guid Id);

internal sealed class PauseCheckHandler(IMonitoringDbContext db)
    : ICommandHandler<PauseCheckCommand, Result<CheckResponse>>
{
    public async Task<Result<CheckResponse>> HandleAsync(PauseCheckCommand command, CancellationToken cancellationToken)
    {
        var check = await db.Checks.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (check is null)
        {
            return CheckErrors.NotFound(command.Id);
        }

        var paused = check.Pause();
        if (paused.IsFailure)
        {
            return paused.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return CheckResponse.From(check);
    }
}

internal sealed class ResumeCheckHandler(IMonitoringDbContext db, TimeProvider clock)
    : ICommandHandler<ResumeCheckCommand, Result<CheckResponse>>
{
    public async Task<Result<CheckResponse>> HandleAsync(ResumeCheckCommand command, CancellationToken cancellationToken)
    {
        var check = await db.Checks.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (check is null)
        {
            return CheckErrors.NotFound(command.Id);
        }

        var resumed = check.Resume(clock.GetUtcNow());
        if (resumed.IsFailure)
        {
            return resumed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return CheckResponse.From(check);
    }
}
