using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Application.Checks;

public sealed record CheckResponse(
    Guid Id,
    string Name,
    string Url,
    int IntervalSeconds,
    int FailureThreshold,
    string Status,
    int ConsecutiveFailures,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastCheckedAt,
    DateTimeOffset NextCheckAt)
{
    internal static CheckResponse From(Check check) => new(
        check.Id,
        check.Name,
        check.Url.AbsoluteUri,
        (int)check.Interval.TotalSeconds,
        check.FailureThreshold,
        check.Status.ToString(),
        check.ConsecutiveFailures,
        check.CreatedAt,
        check.LastCheckedAt,
        check.NextCheckAt);
}

public sealed record CheckResultResponse(
    Guid Id,
    DateTimeOffset CheckedAt,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? Error);
