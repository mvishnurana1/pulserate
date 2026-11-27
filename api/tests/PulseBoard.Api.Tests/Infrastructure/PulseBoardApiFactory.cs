using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PulseBoard.Monitoring.Application.Abstractions;
using Testcontainers.PostgreSql;

namespace PulseBoard.Api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory against a real Postgres in Docker. The only fake is the prober, so tests
/// decide what each endpoint "returns" without touching the network.
/// </summary>
public sealed class PulseBoardApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public FakeEndpointProber Prober { get; } = new();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development applies both modules' migrations on startup, just like running locally.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:pulseboard-db", _postgres.GetConnectionString());

        builder.ConfigureTestServices(services => services.AddSingleton<IEndpointProber>(Prober));
    }
}

/// <summary>Shares one API and one database across all test classes; Docker containers are slow to start.</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PulseBoardApiFactory>
{
    public const string Name = "api";
}
