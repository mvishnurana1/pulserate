using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Tests.Domain;

public sealed class CheckTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    private static Check CreateCheck(int failureThreshold = 3) =>
        Check.Create("Example", "https://example.com", OneMinute, failureThreshold, Now).Value;

    private static CheckOutcome Succeeded() => CheckOutcome.Success(200, TimeSpan.FromMilliseconds(80));

    private static CheckOutcome Failed() => CheckOutcome.Failure("Service unavailable", TimeSpan.FromMilliseconds(120), 503);

    [Fact]
    public void New_check_is_pending_and_due_straight_away()
    {
        var check = CreateCheck();

        Assert.Equal(CheckStatus.Pending, check.Status);
        Assert.True(check.IsDue(Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_name(string name)
    {
        var result = Check.Create(name, "https://example.com", OneMinute, 3, Now);

        Assert.Equal(CheckErrors.NameRequired, result.Error);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("example.com")]
    [InlineData("ftp://example.com")]
    public void Create_rejects_anything_but_absolute_http_urls(string url)
    {
        var result = Check.Create("Example", url, OneMinute, 3, Now);

        Assert.Equal(CheckErrors.InvalidUrl, result.Error);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(7200)]
    public void Create_rejects_intervals_outside_the_allowed_range(int seconds)
    {
        var result = Check.Create("Example", "https://example.com", TimeSpan.FromSeconds(seconds), 3, Now);

        Assert.Equal(CheckErrors.IntervalOutOfRange, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_rejects_failure_thresholds_outside_the_allowed_range(int threshold)
    {
        var result = Check.Create("Example", "https://example.com", OneMinute, threshold, Now);

        Assert.Equal(CheckErrors.FailureThresholdOutOfRange, result.Error);
    }

    [Fact]
    public void Goes_down_only_when_failures_reach_the_threshold()
    {
        var check = CreateCheck(failureThreshold: 3);

        check.RecordResult(Failed(), Now);
        check.RecordResult(Failed(), Now.AddMinutes(1));

        Assert.NotEqual(CheckStatus.Down, check.Status);
        Assert.Empty(check.DomainEvents.OfType<CheckWentDown>());

        check.RecordResult(Failed(), Now.AddMinutes(2));

        Assert.Equal(CheckStatus.Down, check.Status);
        var wentDown = Assert.Single(check.DomainEvents.OfType<CheckWentDown>());
        Assert.Equal(3, wentDown.ConsecutiveFailures);
        Assert.Equal("Service unavailable", wentDown.LastError);
    }

    [Fact]
    public void Further_failures_while_down_do_not_raise_more_events()
    {
        var check = CreateCheck(failureThreshold: 1);

        check.RecordResult(Failed(), Now);
        check.RecordResult(Failed(), Now.AddMinutes(1));
        check.RecordResult(Failed(), Now.AddMinutes(2));

        Assert.Single(check.DomainEvents.OfType<CheckWentDown>());
        Assert.Equal(3, check.ConsecutiveFailures);
    }

    [Fact]
    public void Success_after_being_down_raises_recovered_and_resets_failures()
    {
        var check = CreateCheck(failureThreshold: 1);
        check.RecordResult(Failed(), Now);
        check.ClearDomainEvents();

        check.RecordResult(Succeeded(), Now.AddMinutes(1));

        Assert.Equal(CheckStatus.Up, check.Status);
        Assert.Equal(0, check.ConsecutiveFailures);
        Assert.Single(check.DomainEvents.OfType<CheckRecovered>());
    }

    [Fact]
    public void Success_while_up_raises_no_status_change_events()
    {
        var check = CreateCheck();

        check.RecordResult(Succeeded(), Now);
        check.RecordResult(Succeeded(), Now.AddMinutes(1));

        Assert.Equal(CheckStatus.Up, check.Status);
        Assert.Empty(check.DomainEvents.OfType<CheckWentDown>());
        Assert.Empty(check.DomainEvents.OfType<CheckRecovered>());
    }

    [Fact]
    public void Every_result_raises_a_result_recorded_event_before_any_status_change()
    {
        var check = CreateCheck(failureThreshold: 1);

        var result = check.RecordResult(Failed(), Now);

        var events = check.DomainEvents.ToList();
        var recorded = Assert.IsType<CheckResultRecorded>(events[0]);
        Assert.IsType<CheckWentDown>(events[1]);
        Assert.Equal(result.Value.Id, recorded.ResultId);
        Assert.False(recorded.IsSuccess);
        Assert.Equal(503, recorded.StatusCode);
    }

    [Fact]
    public void Scheduling_claims_the_check_for_one_interval()
    {
        var check = CreateCheck();
        var scheduledAt = Now.AddSeconds(3);

        check.MarkScheduled(scheduledAt);

        Assert.False(check.IsDue(scheduledAt));
        Assert.True(check.IsDue(scheduledAt + OneMinute));
    }

    [Fact]
    public void Paused_checks_cannot_be_scheduled()
    {
        var check = CreateCheck();
        check.Pause();

        Assert.Equal(CheckErrors.IsPaused, check.MarkScheduled(Now).Error);
    }

    [Fact]
    public void Recording_a_result_schedules_the_next_check()
    {
        var check = CreateCheck();
        var checkedAt = Now.AddSeconds(5);

        var result = check.RecordResult(Succeeded(), checkedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(checkedAt, check.LastCheckedAt);
        Assert.Equal(checkedAt + OneMinute, check.NextCheckAt);
        Assert.False(check.IsDue(checkedAt));
        Assert.True(check.IsDue(checkedAt + OneMinute));
    }

    [Fact]
    public void Long_errors_are_truncated_in_history()
    {
        var check = CreateCheck();
        var longError = new string('x', CheckResult.ErrorMaxLength + 500);

        var result = check.RecordResult(CheckOutcome.Failure(longError, TimeSpan.FromSeconds(10)), Now);

        Assert.Equal(CheckResult.ErrorMaxLength, result.Value.Error!.Length);
    }

    [Fact]
    public void Paused_check_is_never_due_and_ignores_late_results()
    {
        var check = CreateCheck();
        check.Pause();

        Assert.False(check.IsDue(Now.AddDays(1)));
        Assert.Equal(CheckErrors.IsPaused, check.RecordResult(Succeeded(), Now).Error);
    }

    [Fact]
    public void Pausing_twice_is_a_conflict()
    {
        var check = CreateCheck();
        check.Pause();

        Assert.Equal(CheckErrors.AlreadyPaused, check.Pause().Error);
    }

    [Fact]
    public void Resuming_makes_the_check_pending_and_due()
    {
        var check = CreateCheck();
        check.RecordResult(Succeeded(), Now);
        check.Pause();

        var resumedAt = Now.AddMinutes(10);
        check.Resume(resumedAt);

        Assert.Equal(CheckStatus.Pending, check.Status);
        Assert.True(check.IsDue(resumedAt));
    }

    [Fact]
    public void Resuming_an_active_check_is_a_conflict()
    {
        var check = CreateCheck();

        Assert.Equal(CheckErrors.NotPaused, check.Resume(Now).Error);
    }
}
