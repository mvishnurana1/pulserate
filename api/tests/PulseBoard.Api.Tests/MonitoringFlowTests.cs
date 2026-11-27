using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PulseBoard.Api.Tests.Infrastructure;
using PulseBoard.Incidents.Application.Incidents;
using PulseBoard.Monitoring.Application.Checks;
using PulseBoard.Monitoring.Contracts;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Api.Tests;

/// <summary>
/// End-to-end flows across both modules: a result is saved with its events in the outbox, the background
/// processor publishes them, and the Incidents module reacts. Nothing here calls the modules directly
/// except to run a check, which is what the Functions app does.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class MonitoringFlowTests(PulseBoardApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task A_check_going_down_opens_an_incident_and_recovery_resolves_it()
    {
        var url = TestHelpers.UniqueUrl();
        var check = await _client.CreateCheckAsync(url, failureThreshold: 1);

        factory.Prober.Respond(url, CheckOutcome.Failure("HTTP 503 Service Unavailable", TimeSpan.FromMilliseconds(80), 503));
        await factory.RunCheckAsync(check.Id);

        var opened = await TestHelpers.EventuallyAsync(() => FindIncidentAsync(check.Id, "open"));
        Assert.Equal("Open", opened.Status);
        Assert.Equal(check.Name, opened.CheckName);
        Assert.Equal("HTTP 503 Service Unavailable", opened.LastError);

        factory.Prober.Respond(url, CheckOutcome.Success(200, TimeSpan.FromMilliseconds(60)));
        await factory.RunCheckAsync(check.Id);

        var resolved = await TestHelpers.EventuallyAsync(() => FindIncidentAsync(check.Id, "resolved"));
        Assert.Equal(opened.Id, resolved.Id);
        Assert.Equal("Recovered", resolved.Resolution);
        Assert.NotNull(resolved.DurationSeconds);
    }

    [Fact]
    public async Task Failures_below_the_threshold_do_not_open_an_incident()
    {
        var url = TestHelpers.UniqueUrl();
        var check = await _client.CreateCheckAsync(url, failureThreshold: 3);

        factory.Prober.Respond(url, CheckOutcome.Failure("Timed out after 10 seconds", TimeSpan.FromSeconds(10)));
        await factory.RunCheckAsync(check.Id);
        await factory.RunCheckAsync(check.Id);

        // Give the outbox processor time to publish anything it was going to.
        await Task.Delay(TimeSpan.FromSeconds(3));

        Assert.Null(await FindIncidentAsync(check.Id, status: null));
        var refreshed = await _client.GetFromJsonAsync<CheckResponse>($"/api/checks/{check.Id}");
        Assert.Equal(2, refreshed?.ConsecutiveFailures);
        Assert.NotEqual("Down", refreshed?.Status);
    }

    [Fact]
    public async Task A_redelivered_event_does_not_open_a_second_incident()
    {
        var checkId = Guid.NewGuid();
        var wentDown = new CheckWentDownIntegrationEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, checkId, "Duplicate test", TestHelpers.UniqueUrl(), 3, "HTTP 500");

        // At-least-once delivery means the same event can arrive twice; the handler must be idempotent.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();
            await dispatcher.DispatchAsync(wentDown, CancellationToken.None);
            await dispatcher.DispatchAsync(wentDown, CancellationToken.None);
        }

        var incidents = await _client.GetFromJsonAsync<List<IncidentResponse>>("/api/incidents?status=open&take=500");
        Assert.Single(incidents!, incident => incident.CheckId == checkId);
    }

    [Fact]
    public async Task The_scheduler_claims_a_due_check_only_once_per_interval()
    {
        var check = await _client.CreateCheckAsync();

        var first = await ScheduleDueChecksAsync();
        var second = await ScheduleDueChecksAsync();

        Assert.Contains(check.Id, first);
        Assert.DoesNotContain(check.Id, second);
    }

    [Fact]
    public async Task Paused_checks_are_never_scheduled()
    {
        var check = await _client.CreateCheckAsync();
        await _client.PostAsync($"/api/checks/{check.Id}/pause", null);

        Assert.DoesNotContain(check.Id, await ScheduleDueChecksAsync());
    }

    private async Task<IncidentResponse?> FindIncidentAsync(Guid checkId, string? status)
    {
        var query = status is null ? "/api/incidents?take=500" : $"/api/incidents?status={status}&take=500";
        var incidents = await _client.GetFromJsonAsync<List<IncidentResponse>>(query);
        return incidents?.FirstOrDefault(incident => incident.CheckId == checkId);
    }

    private async Task<IReadOnlyList<Guid>> ScheduleDueChecksAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ScheduleDueChecksCommand, Result<IReadOnlyList<Guid>>>>();
        var result = await handler.HandleAsync(new ScheduleDueChecksCommand(BatchSize: 1000), CancellationToken.None);
        return result.Value;
    }
}
