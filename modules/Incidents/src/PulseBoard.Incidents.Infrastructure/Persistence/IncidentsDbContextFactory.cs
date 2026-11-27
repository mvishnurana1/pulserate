using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseBoard.Incidents.Infrastructure.Persistence;

/// <summary>Used only by the <c>dotnet ef</c> tools to build migrations. It never opens a connection.</summary>
public sealed class IncidentsDbContextFactory : IDesignTimeDbContextFactory<IncidentsDbContext>
{
    public IncidentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IncidentsDbContext>()
            .UseNpgsql("Host=localhost;Database=pulseboard", IncidentsModule.ConfigureNpgsql)
            .Options;

        return new IncidentsDbContext(options);
    }
}
