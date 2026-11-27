using PulseBoard.Incidents.Domain.Incidents;

namespace PulseBoard.Incidents.Tests.Domain;

public sealed class IncidentTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

    private static Incident OpenIncident(string? lastError = "HTTP 503") =>
        Incident.Open(Guid.NewGuid(), "Example", "https://example.com/", 3, lastError, OpenedAt);

    [Fact]
    public void Opens_with_details_from_the_check()
    {
        var incident = OpenIncident();

        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Equal("Example", incident.CheckName);
        Assert.Equal(3, incident.FailureCount);
        Assert.Null(incident.ResolvedAt);
        Assert.Null(incident.Duration);
    }

    [Fact]
    public void Resolving_records_when_how_and_how_long()
    {
        var incident = OpenIncident();

        var result = incident.Resolve(IncidentResolution.Recovered, OpenedAt.AddMinutes(7));

        Assert.True(result.IsSuccess);
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(IncidentResolution.Recovered, incident.Resolution);
        Assert.Equal(TimeSpan.FromMinutes(7), incident.Duration);
    }

    [Fact]
    public void Resolving_twice_is_a_conflict()
    {
        var incident = OpenIncident();
        incident.Resolve(IncidentResolution.Manual, OpenedAt.AddMinutes(1));

        Assert.Equal(IncidentErrors.AlreadyResolved, incident.Resolve(IncidentResolution.Recovered, OpenedAt.AddMinutes(2)).Error);
    }

    [Fact]
    public void A_recovery_event_older_than_the_incident_never_gives_a_negative_duration()
    {
        var incident = OpenIncident();

        incident.Resolve(IncidentResolution.Recovered, OpenedAt.AddSeconds(-5));

        Assert.Equal(TimeSpan.Zero, incident.Duration);
    }

    [Fact]
    public void Long_errors_are_truncated()
    {
        var incident = OpenIncident(new string('x', Incident.ErrorMaxLength + 100));

        Assert.Equal(Incident.ErrorMaxLength, incident.LastError!.Length);
    }
}
