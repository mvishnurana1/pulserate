using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Infrastructure.Persistence;

/// <summary>
/// Owns the "monitoring" schema. No other module reads or writes these tables.
/// </summary>
public sealed class MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
    : DbContext(options), IMonitoringDbContext
{
    public const string Schema = "monitoring";

    public DbSet<Check> Checks => Set<Check>();

    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MonitoringDbContext).Assembly);
    }
}
