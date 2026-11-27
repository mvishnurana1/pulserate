using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using PulseBoard.Functions.Checks;
using PulseBoard.Monitoring.Application.Checks;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Functions.Tests;

/// <summary>
/// The functions are thin adapters, so these tests check the adapting: what goes onto the queue,
/// and that expected failures complete the message instead of throwing (which would trigger retries).
/// </summary>
public sealed class CheckFunctionsTests
{
    [Fact]
    public async Task Scheduler_queues_one_message_per_due_check()
    {
        var due = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var function = new ScheduleDueChecksFunction(
            new StubHandler<ScheduleDueChecksCommand, Result<IReadOnlyList<Guid>>>(Result.Success<IReadOnlyList<Guid>>(due)),
            NullLogger<ScheduleDueChecksFunction>.Instance);

        var messages = await function.Run(new TimerInfo(), CancellationToken.None);

        var queued = messages.Select(m => JsonSerializer.Deserialize<CheckRequest>(m)!.CheckId);
        Assert.Equal(due, queued);
    }

    [Fact]
    public async Task Scheduler_queues_nothing_when_no_checks_are_due()
    {
        var function = new ScheduleDueChecksFunction(
            new StubHandler<ScheduleDueChecksCommand, Result<IReadOnlyList<Guid>>>(Result.Success<IReadOnlyList<Guid>>([])),
            NullLogger<ScheduleDueChecksFunction>.Instance);

        Assert.Empty(await function.Run(new TimerInfo(), CancellationToken.None));
    }

    [Fact]
    public async Task A_check_deleted_after_it_was_queued_completes_without_throwing()
    {
        var checkId = Guid.NewGuid();
        var function = new RunCheckFunction(
            new StubHandler<RunCheckCommand, Result<CheckResultResponse>>(CheckErrors.NotFound(checkId)),
            NullLogger<RunCheckFunction>.Instance);

        // No exception means the queue message completes rather than being retried five times.
        await function.Run(new CheckRequest(checkId), CancellationToken.None);
    }

    private sealed class StubHandler<TCommand, TResult>(TResult result) : ICommandHandler<TCommand, TResult>
        where TResult : Result
    {
        public Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken) => Task.FromResult(result);
    }
}
