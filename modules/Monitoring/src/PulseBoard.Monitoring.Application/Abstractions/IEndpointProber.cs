using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Application.Abstractions;

/// <summary>
/// Calls an endpoint once and reports what happened. Never throws for network failures:
/// a timeout or refused connection is a normal (failed) outcome, not an exception.
/// </summary>
public interface IEndpointProber
{
    Task<CheckOutcome> ProbeAsync(Uri url, CancellationToken cancellationToken);
}
