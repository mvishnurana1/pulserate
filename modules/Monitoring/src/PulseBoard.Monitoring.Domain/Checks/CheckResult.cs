namespace PulseBoard.Monitoring.Domain.Checks;

/// <summary>
/// One entry in a check's history. Append-only, so it lives outside the <see cref="Check"/> aggregate
/// rather than as a collection on it (a busy check produces thousands of these).
/// </summary>
public sealed class CheckResult
{
    public const int ErrorMaxLength = 1000;

    private CheckResult()
    {
        // Required by EF Core.
    }

    public Guid Id { get; private set; }

    public Guid CheckId { get; private set; }

    public DateTimeOffset CheckedAt { get; private set; }

    public bool IsSuccess { get; private set; }

    public int? StatusCode { get; private set; }

    public int ResponseTimeMs { get; private set; }

    public string? Error { get; private set; }

    internal static CheckResult Create(Guid checkId, CheckOutcome outcome, DateTimeOffset checkedAt) => new()
    {
        Id = Guid.CreateVersion7(checkedAt),
        CheckId = checkId,
        CheckedAt = checkedAt,
        IsSuccess = outcome.IsSuccess,
        StatusCode = outcome.StatusCode,
        ResponseTimeMs = (int)Math.Round(outcome.ResponseTime.TotalMilliseconds),
        Error = outcome.Error is { Length: > ErrorMaxLength } error ? error[..ErrorMaxLength] : outcome.Error,
    };
}
