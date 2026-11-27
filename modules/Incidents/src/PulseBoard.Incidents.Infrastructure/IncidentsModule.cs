using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using PulseBoard.Incidents.Application;
using PulseBoard.Incidents.Application.Abstractions;
using PulseBoard.Incidents.Infrastructure.Persistence;

namespace PulseBoard.Incidents.Infrastructure;

public static class IncidentsModule
{
    /// <summary>Same database as Monitoring, but a separate schema and migration history.</summary>
    public const string ConnectionName = "pulseboard-db";

    public static IHostApplicationBuilder AddIncidentsModule(this IHostApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddIncidentsApplication();

        builder.Services.AddDbContextPool<IncidentsDbContext>(options => options.UseNpgsql(
            builder.Configuration.GetConnectionString(ConnectionName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionName}' is missing."),
            ConfigureNpgsql));

        builder.EnrichNpgsqlDbContext<IncidentsDbContext>();

        builder.Services.AddScoped<IIncidentsDbContext>(sp => sp.GetRequiredService<IncidentsDbContext>());

        return builder;
    }

    /// <summary>Applies pending migrations. Development only.</summary>
    public static async Task MigrateIncidentsDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IncidentsDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    internal static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, IncidentsDbContext.Schema);
}
