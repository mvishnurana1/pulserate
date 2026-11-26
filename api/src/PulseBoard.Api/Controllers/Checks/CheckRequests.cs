using PulseBoard.Monitoring.Application.Checks;

namespace PulseBoard.Api.Controllers.Checks;

// HTTP contracts are kept separate from commands, so the API shape can change without touching the module.

public sealed record CreateCheckRequest(string Name, string Url, int IntervalSeconds = 60, int FailureThreshold = 3)
{
    public CreateCheckCommand ToCommand() => new(Name, Url, IntervalSeconds, FailureThreshold);
}

public sealed record UpdateCheckRequest(string Name, string Url, int IntervalSeconds, int FailureThreshold)
{
    public UpdateCheckCommand ToCommand(Guid id) => new(id, Name, Url, IntervalSeconds, FailureThreshold);
}
