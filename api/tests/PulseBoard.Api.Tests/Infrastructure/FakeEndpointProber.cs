using System.Collections.Concurrent;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Api.Tests.Infrastructure;

/// <summary>
/// Returns whatever outcome a test set for a URL, or a healthy 200 by default.
/// Each test uses its own unique URL, so tests running in the same API don't interfere.
/// </summary>
public sealed class FakeEndpointProber : IEndpointProber
{
    private readonly ConcurrentDictionary<string, CheckOutcome> _outcomes = new();

    public void Respond(string url, CheckOutcome outcome) => _outcomes[new Uri(url).AbsoluteUri] = outcome;

    public Task<CheckOutcome> ProbeAsync(Uri url, CancellationToken cancellationToken) =>
        Task.FromResult(_outcomes.TryGetValue(url.AbsoluteUri, out var outcome)
            ? outcome
            : CheckOutcome.Success(200, TimeSpan.FromMilliseconds(42)));
}
