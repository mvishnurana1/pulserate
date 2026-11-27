using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PulseBoard.Monitoring.Application.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Api.Tests.Infrastructure;

internal static class TestHelpers
{
    /// <summary>A URL no other test uses, so fake outcomes and incident lookups stay isolated.</summary>
    public static string UniqueUrl() => $"https://{Guid.NewGuid():N}.example.test/health";

    public static async Task<CheckResponse> CreateCheckAsync(this HttpClient client, string? url = null, int failureThreshold = 3)
    {
        var response = await client.PostAsJsonAsync("/api/checks", new
        {
            name = $"Test {Guid.NewGuid():N}"[..20],
            url = url ?? UniqueUrl(),
            intervalSeconds = 60,
            failureThreshold,
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckResponse>())!;
    }

    /// <summary>Runs one check through the same handler the Functions app uses.</summary>
    public static async Task<Result<CheckResultResponse>> RunCheckAsync(this PulseBoardApiFactory factory, Guid checkId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RunCheckCommand, Result<CheckResultResponse>>>();
        return await handler.HandleAsync(new RunCheckCommand(checkId), CancellationToken.None);
    }

    public static async Task<string?> ReadErrorCodeAsync(this HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problem?.Extensions.TryGetValue("code", out var code) == true ? code?.ToString() : null;
    }

    /// <summary>
    /// Polls until <paramref name="probe"/> returns a value. Used for work that happens in the background,
    /// such as the outbox processor publishing events about a second after they're written.
    /// </summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T?>> probe, TimeSpan? timeout = null)
        where T : class
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));

        while (true)
        {
            if (await probe() is { } value)
            {
                return value;
            }

            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The expected background work didn't happen in time.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }
    }
}
