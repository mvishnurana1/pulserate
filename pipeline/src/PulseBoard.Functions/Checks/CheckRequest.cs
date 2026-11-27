namespace PulseBoard.Functions.Checks;

/// <summary>Queue message asking for one run of a check. Only the ID: the worker loads fresh settings.</summary>
public sealed record CheckRequest(Guid CheckId);

public static class QueueNames
{
    public const string CheckRequests = "check-requests";

    /// <summary>Connection name. Matches the queue resource in the Aspire AppHost and local.settings.json.</summary>
    public const string Connection = "queues";
}
