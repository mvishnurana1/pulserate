using PulseBoard.SharedKernel;

namespace PulseBoard.Incidents.Domain.Incidents;

public static class IncidentErrors
{
    public static readonly Error AlreadyResolved =
        Error.Conflict("Incident.AlreadyResolved", "The incident is already resolved.");

    public static readonly Error InvalidStatusFilter =
        Error.Validation("Incident.InvalidStatusFilter", "Status must be 'open' or 'resolved'.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Incident.NotFound", $"Incident '{id}' was not found.");
}
