using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Infrastructure.Probing;

/// <summary>
/// Probes an endpoint with a single GET. Deliberately has no retry policy: retrying would hide
/// outages and inflate response times. The check's failure threshold is what absorbs one-off blips.
/// Connections go through <see cref="SafeSocketConnector"/>, which blocks private and reserved addresses.
/// </summary>
public sealed class HttpEndpointProber(HttpClient httpClient, TimeProvider clock) : IEndpointProber
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public async Task<CheckOutcome> ProbeAsync(Uri url, CancellationToken cancellationToken)
    {
        var started = clock.GetTimestamp();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Only the status line and headers matter, so don't wait to download the body.
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var elapsed = clock.GetElapsedTime(started);
            var statusCode = (int)response.StatusCode;

            return response.IsSuccessStatusCode
                ? CheckOutcome.Success(statusCode, elapsed)
                : CheckOutcome.Failure($"HTTP {statusCode} {response.ReasonPhrase}".TrimEnd(), elapsed, statusCode);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CheckOutcome.Failure($"Timed out after {Timeout.TotalSeconds:0} seconds", clock.GetElapsedTime(started));
        }
        catch (HttpRequestException ex)
        {
            // HttpClient wraps errors from the connect callback, so surface the "blocked" reason if that's what happened.
            var message = FindBlocked(ex)?.Message ?? ex.Message;
            return CheckOutcome.Failure(message, clock.GetElapsedTime(started));
        }
    }

    private static BlockedAddressException? FindBlocked(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is BlockedAddressException blocked)
            {
                return blocked;
            }
        }

        return null;
    }
}
