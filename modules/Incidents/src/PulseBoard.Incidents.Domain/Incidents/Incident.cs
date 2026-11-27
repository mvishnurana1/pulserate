using PulseBoard.SharedKernel;

namespace PulseBoard.Incidents.Domain.Incidents;

public enum IncidentStatus
{
    Open = 0,
    Resolved,
}

public enum IncidentResolution
{
    /// <summary>The check succeeded again.</summary>
    Recovered = 0,

    /// <summary>Someone closed it by hand (for example after pausing or deleting the check).</summary>
    Manual,
}

/// <summary>
/// A period during which a monitored endpoint was down. Opened and resolved from Monitoring's events.
/// Keeps its own copy of the check's name and URL, so it reads well even after the check is renamed or deleted.
/// </summary>
public sealed class Incident : Entity<Guid>
{
    public const int CheckNameMaxLength = 100;
    public const int UrlMaxLength = 2048;
    public const int ErrorMaxLength = 1000;

    private Incident()
    {
        // Required by EF Core.
    }

    public Guid CheckId { get; private set; }

    public string CheckName { get; private set; } = string.Empty;

    public string Url { get; private set; } = string.Empty;

    public IncidentStatus Status { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public IncidentResolution? Resolution { get; private set; }

    /// <summary>Consecutive failures when the incident opened.</summary>
    public int FailureCount { get; private set; }

    public string? LastError { get; private set; }

    public TimeSpan? Duration => ResolvedAt - OpenedAt;

    public static Incident Open(
        Guid checkId,
        string checkName,
        string url,
        int failureCount,
        string? lastError,
        DateTimeOffset openedAt) => new()
    {
        Id = Guid.CreateVersion7(openedAt),
        CheckId = checkId,
        CheckName = Truncate(checkName, CheckNameMaxLength)!,
        Url = Truncate(url, UrlMaxLength)!,
        Status = IncidentStatus.Open,
        OpenedAt = openedAt,
        FailureCount = failureCount,
        LastError = Truncate(lastError, ErrorMaxLength),
    };

    public Result Resolve(IncidentResolution resolution, DateTimeOffset resolvedAt)
    {
        if (Status == IncidentStatus.Resolved)
        {
            return IncidentErrors.AlreadyResolved;
        }

        Status = IncidentStatus.Resolved;
        Resolution = resolution;

        // Events can arrive slightly out of order; never produce a negative duration.
        ResolvedAt = resolvedAt < OpenedAt ? OpenedAt : resolvedAt;
        return Result.Success();
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: var length } && length > maxLength ? value[..maxLength] : value;
}
