namespace PulseBoard.SharedKernel.Messaging;

/// <summary>
/// Handles a request that changes state. Always returns a <see cref="Result"/> so callers handle expected failures.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TResult : Result
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a read-only request.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
