using PulseBoard.SharedKernel;

namespace PulseBoard.Monitoring.Domain.Checks;

/// <summary>
/// An HTTP endpoint that PulseBoard checks on a schedule.
/// Owns the rules for scheduling and for deciding when the endpoint counts as up or down.
/// </summary>
public sealed class Check : Entity<Guid>
{
    public const int NameMaxLength = 100;
    public const int UrlMaxLength = 2048;
    public const int MinFailureThreshold = 1;
    public const int MaxFailureThreshold = 10;

    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromHours(1);

    private Check()
    {
        // Required by EF Core.
    }

    public string Name { get; private set; } = string.Empty;

    public Uri Url { get; private set; } = null!;

    public TimeSpan Interval { get; private set; }

    /// <summary>Consecutive failed checks needed before the endpoint is marked down.</summary>
    public int FailureThreshold { get; private set; }

    public CheckStatus Status { get; private set; }

    public int ConsecutiveFailures { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastCheckedAt { get; private set; }

    public DateTimeOffset NextCheckAt { get; private set; }

    public bool IsPaused => Status == CheckStatus.Paused;

    public static Result<Check> Create(
        string name,
        string url,
        TimeSpan interval,
        int failureThreshold,
        DateTimeOffset now)
    {
        var validated = ValidateSettings(name, url, interval, failureThreshold);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        return new Check
        {
            Id = Guid.CreateVersion7(now),
            Name = name.Trim(),
            Url = validated.Value,
            Interval = interval,
            FailureThreshold = failureThreshold,
            Status = CheckStatus.Pending,
            CreatedAt = now,
            NextCheckAt = now, // New checks run straight away.
        };
    }

    public Result Update(string name, string url, TimeSpan interval, int failureThreshold, DateTimeOffset now)
    {
        var validated = ValidateSettings(name, url, interval, failureThreshold);
        if (validated.IsFailure)
        {
            return validated.Error;
        }

        Name = name.Trim();
        Url = validated.Value;
        Interval = interval;
        FailureThreshold = failureThreshold;

        if (!IsPaused)
        {
            NextCheckAt = now; // Re-check soon so the new settings take effect.
        }

        return Result.Success();
    }

    public Result Pause()
    {
        if (IsPaused)
        {
            return CheckErrors.AlreadyPaused;
        }

        Status = CheckStatus.Paused;
        ConsecutiveFailures = 0;
        return Result.Success();
    }

    public Result Resume(DateTimeOffset now)
    {
        if (!IsPaused)
        {
            return CheckErrors.NotPaused;
        }

        Status = CheckStatus.Pending;
        NextCheckAt = now;
        return Result.Success();
    }

    public bool IsDue(DateTimeOffset now) => !IsPaused && NextCheckAt <= now;

    /// <summary>
    /// Applies the outcome of one HTTP check: updates the schedule and status, and raises
    /// <see cref="CheckWentDown"/> or <see cref="CheckRecovered"/> when the status flips.
    /// </summary>
    /// <returns>The history entry to persist.</returns>
    public Result<CheckResult> RecordResult(CheckOutcome outcome, DateTimeOffset checkedAt)
    {
        if (IsPaused)
        {
            // A check that was in flight when the user paused it. Ignore the late result.
            return CheckErrors.IsPaused;
        }

        var result = CheckResult.Create(Id, outcome, checkedAt);
        LastCheckedAt = checkedAt;
        NextCheckAt = checkedAt + Interval;

        if (outcome.IsSuccess)
        {
            MarkSuccess(checkedAt);
        }
        else
        {
            MarkFailure(outcome, checkedAt);
        }

        return result;
    }

    private void MarkSuccess(DateTimeOffset checkedAt)
    {
        var wasDown = Status == CheckStatus.Down;

        Status = CheckStatus.Up;
        ConsecutiveFailures = 0;

        if (wasDown)
        {
            Raise(new CheckRecovered(Id, checkedAt));
        }
    }

    private void MarkFailure(CheckOutcome outcome, DateTimeOffset checkedAt)
    {
        ConsecutiveFailures++;

        // One blip shouldn't page anyone: only flip to Down once the threshold is reached,
        // and only raise the event on the transition, not on every failure after it.
        if (Status != CheckStatus.Down && ConsecutiveFailures >= FailureThreshold)
        {
            Status = CheckStatus.Down;
            Raise(new CheckWentDown(Id, ConsecutiveFailures, outcome.Error, checkedAt));
        }
    }

    private static Result<Uri> ValidateSettings(string name, string url, TimeSpan interval, int failureThreshold)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CheckErrors.NameRequired;
        }

        if (name.Trim().Length > NameMaxLength)
        {
            return CheckErrors.NameTooLong;
        }

        if (string.IsNullOrWhiteSpace(url)
            || url.Length > UrlMaxLength
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return CheckErrors.InvalidUrl;
        }

        if (interval < MinInterval || interval > MaxInterval)
        {
            return CheckErrors.IntervalOutOfRange;
        }

        if (failureThreshold is < MinFailureThreshold or > MaxFailureThreshold)
        {
            return CheckErrors.FailureThresholdOutOfRange;
        }

        return uri;
    }
}
