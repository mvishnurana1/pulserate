using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

public sealed record DeleteCheckCommand(Guid Id);

internal sealed class DeleteCheckHandler(IMonitoringDbContext db) : ICommandHandler<DeleteCheckCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCheckCommand command, CancellationToken cancellationToken)
    {
        var check = await db.Checks.FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (check is null)
        {
            return CheckErrors.NotFound(command.Id);
        }

        // History rows are removed by the database's cascade delete.
        db.Checks.Remove(check);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
