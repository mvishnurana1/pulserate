using PulseBoard.SharedKernel;

namespace PulseBoard.Monitoring.Domain.Checks;

public static class CheckErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Check.NameRequired", "A name is required.");

    public static readonly Error NameTooLong =
        Error.Validation("Check.NameTooLong", $"Name must be {Check.NameMaxLength} characters or fewer.");

    public static readonly Error InvalidUrl =
        Error.Validation("Check.InvalidUrl", "URL must be an absolute http:// or https:// address.");

    public static readonly Error IntervalOutOfRange = Error.Validation(
        "Check.IntervalOutOfRange",
        $"Interval must be between {Check.MinInterval.TotalSeconds:0} seconds and {Check.MaxInterval.TotalMinutes:0} minutes.");

    public static readonly Error FailureThresholdOutOfRange = Error.Validation(
        "Check.FailureThresholdOutOfRange",
        $"Failure threshold must be between {Check.MinFailureThreshold} and {Check.MaxFailureThreshold}.");

    public static readonly Error AlreadyPaused =
        Error.Conflict("Check.AlreadyPaused", "The check is already paused.");

    public static readonly Error NotPaused =
        Error.Conflict("Check.NotPaused", "The check is not paused.");

    public static readonly Error IsPaused =
        Error.Conflict("Check.Paused", "The check is paused, so results are not recorded.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Check.NotFound", $"Check '{id}' was not found.");
}
