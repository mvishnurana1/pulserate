using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

public sealed record UpdateCheckCommand(Guid Id, string Name, string Url, int IntervalSeconds, int FailureThreshold);

internal sealed class UpdateCheckHandler(IMonitoringDbContext db, TimeProvider clock)
    : ICommandHandler<UpdateCheckCommand, Result<CheckResponse>>
{
    public async Task<Result<CheckResponse>> HandleAsync(UpdateCheckCommand command, CancellationToken cancellationToken)
    {
        var check = await db.Checks.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (check is null)
        {
            return CheckErrors.NotFound(command.Id);
        }

        var updated = check.Update(
            command.Name,
            command.Url,
            TimeSpan.FromSeconds(command.IntervalSeconds),
            command.FailureThreshold,
            clock.GetUtcNow());

        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return CheckResponse.From(check);
    }
}
