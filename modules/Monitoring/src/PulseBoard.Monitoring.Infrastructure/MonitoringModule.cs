using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using PulseBoard.Monitoring.Application;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Infrastructure.Outbox;
using PulseBoard.Monitoring.Infrastructure.Persistence;
using PulseBoard.Monitoring.Infrastructure.Probing;

namespace PulseBoard.Monitoring.Infrastructure;

/// <summary>
/// The single entry point a host (API or Functions) uses to plug in the Monitoring module.
/// </summary>
public static class MonitoringModule
{
    /// <summary>Connection string name. Matches the database resource in the Aspire AppHost.</summary>
    public const string ConnectionName = "pulseboard-db";

    public static IHostApplicationBuilder AddMonitoringModule(this IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddMonitoringApplication();

        builder.Services.AddDbContextPool<MonitoringDbContext>(options => options.UseNpgsql(
            builder.Configuration.GetConnectionString(ConnectionName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionName}' is missing."),
            ConfigureNpgsql));

        // Adds Aspire's health check, connection retries and OpenTelemetry tracing for this context.
        builder.EnrichNpgsqlDbContext<MonitoringDbContext>();

        builder.Services.AddScoped<IMonitoringDbContext>(sp => sp.GetRequiredService<MonitoringDbContext>());

        // The prober gets its own HttpClient with no resilience handler. Aspire's ServiceDefaults adds retries
        // to every client by default, which would hide outages and skew response times.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers may be marked experimental.
        builder.Services
            .AddHttpClient<IEndpointProber, HttpEndpointProber>(client =>
            {
                client.Timeout = HttpEndpointProber.Timeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PulseBoard/1.0 (+uptime monitor)");
            })
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        return builder;
    }

    /// <summary>
    /// Publishes the module's outbox to in-process integration event handlers.
    /// Run it in one host type only (the API), where the subscribers live.
    /// </summary>
    public static IHostApplicationBuilder AddMonitoringOutboxProcessor(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<MonitoringOutboxProcessor>();
        return builder;
    }

    /// <summary>Applies pending migrations. Development only; production runs migrations as a deploy step.</summary>
    public static async Task MigrateMonitoringDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    // Each module keeps its migration history in its own schema, so modules can migrate independently.
    internal static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, MonitoringDbContext.Schema);
}
