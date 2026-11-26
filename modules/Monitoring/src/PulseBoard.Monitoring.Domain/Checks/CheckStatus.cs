namespace PulseBoard.Monitoring.Domain.Checks;

public enum CheckStatus
{
    /// <summary>Created or resumed, and no successful check yet.</summary>
    Pending = 0,

    Up,

    /// <summary>Failed at least <see cref="Check.FailureThreshold"/> times in a row.</summary>
    Down,

    Paused,
}
