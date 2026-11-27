using PulseBoard.Incidents.Domain.Incidents;

namespace PulseBoard.Incidents.Application.Incidents;

public sealed record IncidentResponse(
    Guid Id,
    Guid CheckId,
    string CheckName,
    string Url,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ResolvedAt,
    int? DurationSeconds,
    string? Resolution,
    int FailureCount,
    string? LastError)
{
    internal static IncidentResponse From(Incident incident) => new(
        incident.Id,
        incident.CheckId,
        incident.CheckName,
        incident.Url,
        incident.Status.ToString(),
        incident.OpenedAt,
        incident.ResolvedAt,
        incident.Duration is { } duration ? (int)duration.TotalSeconds : null,
        incident.Resolution?.ToString(),
        incident.FailureCount,
        incident.LastError);
}
