using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseBoard.Monitoring.Infrastructure.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tools to build migrations. It never opens a connection,
/// so the placeholder connection string is fine.
/// </summary>
public sealed class MonitoringDbContextFactory : IDesignTimeDbContextFactory<MonitoringDbContext>
{
    public MonitoringDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MonitoringDbContext>()
            .UseNpgsql("Host=localhost;Database=pulseboard", MonitoringModule.ConfigureNpgsql)
            .Options;

        return new MonitoringDbContext(options);
    }
}
