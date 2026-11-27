using Microsoft.AspNetCore.SignalR;
using PulseBoard.Monitoring.Contracts;

namespace PulseBoard.Api.Realtime;

/// <summary>Messages the server pushes to the dashboard. Method names become the client-side event names.</summary>
public interface IChecksClient
{
    Task CheckResultRecorded(CheckResultRecordedIntegrationEvent update);

    Task CheckWentDown(CheckWentDownIntegrationEvent update);

    Task CheckRecovered(CheckRecoveredIntegrationEvent update);
}

/// <summary>
/// Server-to-client only: the dashboard listens here and uses the REST API for everything else.
/// </summary>
public sealed class ChecksHub : Hub<IChecksClient>
{
    public const string Path = "/hubs/checks";
}
