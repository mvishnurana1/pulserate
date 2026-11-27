using Microsoft.AspNetCore.SignalR;
using PulseBoard.Monitoring.Contracts;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Api.Realtime;

/// <summary>
/// Forwards Monitoring's integration events to connected dashboards. Just another subscriber of the
/// outbox, alongside the Incidents module, so the domain knows nothing about SignalR.
/// </summary>
internal sealed class RealtimeNotifier(IHubContext<ChecksHub, IChecksClient> hub) :
    IIntegrationEventHandler<CheckResultRecordedIntegrationEvent>,
    IIntegrationEventHandler<CheckWentDownIntegrationEvent>,
    IIntegrationEventHandler<CheckRecoveredIntegrationEvent>
{
    public Task HandleAsync(CheckResultRecordedIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        hub.Clients.All.CheckResultRecorded(integrationEvent);

    public Task HandleAsync(CheckWentDownIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        hub.Clients.All.CheckWentDown(integrationEvent);

    public Task HandleAsync(CheckRecoveredIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        hub.Clients.All.CheckRecovered(integrationEvent);
}
