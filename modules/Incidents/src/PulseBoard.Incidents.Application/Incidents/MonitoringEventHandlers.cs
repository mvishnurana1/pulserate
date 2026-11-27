using Microsoft.EntityFrameworkCore;
using PulseBoard.Incidents.Application.Abstractions;
using PulseBoard.Incidents.Domain.Incidents;
using PulseBoard.Monitoring.Contracts;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Incidents.Application.Incidents;

// Incidents reacts to Monitoring through its public integration events only.
// Both handlers are idempotent because the outbox delivers at least once.

internal sealed class OpenIncidentWhenCheckGoesDownHandler(IIncidentsDbContext db)
    : IIntegrationEventHandler<CheckWentDownIntegrationEvent>
{
    public async Task HandleAsync(CheckWentDownIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var alreadyOpen = await db.Incidents.AnyAsync(
            i => i.CheckId == integrationEvent.CheckId && i.Status == IncidentStatus.Open,
            cancellationToken);

        if (alreadyOpen)
        {
            return; // Redelivered event, or a duplicate. A unique partial index backs this up.
        }

        db.Incidents.Add(Incident.Open(
            integrationEvent.CheckId,
            integrationEvent.CheckName,
            integrationEvent.Url,
            integrationEvent.ConsecutiveFailures,
            integrationEvent.LastError,
            integrationEvent.OccurredAt));

        await db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class ResolveIncidentWhenCheckRecoversHandler(IIncidentsDbContext db)
    : IIntegrationEventHandler<CheckRecoveredIntegrationEvent>
{
    public async Task HandleAsync(CheckRecoveredIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(
            i => i.CheckId == integrationEvent.CheckId && i.Status == IncidentStatus.Open,
            cancellationToken);

        if (incident is null)
        {
            return; // Already resolved (redelivery, or someone resolved it by hand).
        }

        incident.Resolve(IncidentResolution.Recovered, integrationEvent.OccurredAt);
        await db.SaveChangesAsync(cancellationToken);
    }
}
