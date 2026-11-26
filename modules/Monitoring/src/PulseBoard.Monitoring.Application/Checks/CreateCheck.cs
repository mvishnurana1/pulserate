using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

public sealed record CreateCheckCommand(string Name, string Url, int IntervalSeconds, int FailureThreshold);

internal sealed class CreateCheckHandler(IMonitoringDbContext db, TimeProvider clock)
    : ICommandHandler<CreateCheckCommand, Result<CheckResponse>>
{
    public async Task<Result<CheckResponse>> HandleAsync(CreateCheckCommand command, CancellationToken cancellationToken)
    {
        var created = Check.Create(
            command.Name,
            command.Url,
            TimeSpan.FromSeconds(command.IntervalSeconds),
            command.FailureThreshold,
            clock.GetUtcNow());

        if (created.IsFailure)
        {
            return created.Error;
        }

        db.Checks.Add(created.Value);
        await db.SaveChangesAsync(cancellationToken);

        return CheckResponse.From(created.Value);
    }
}
