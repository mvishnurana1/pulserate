using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.Monitoring.Infrastructure.Outbox;

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

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        MoveDomainEventsToOutbox();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MonitoringDbContext).Assembly);
    }

    /// <summary>
    /// Turns domain events on tracked checks into outbox rows, so they commit atomically with the change.
    /// </summary>
    private void MoveDomainEventsToOutbox()
    {
        var checksWithEvents = ChangeTracker.Entries<Check>()
            .Select(entry => entry.Entity)
            .Where(check => check.DomainEvents.Count > 0)
            .ToList();

        foreach (var check in checksWithEvents)
        {
            foreach (var domainEvent in check.DomainEvents)
            {
                OutboxMessages.Add(OutboxMessage.From(IntegrationEventMapper.Map(check, domainEvent)));
            }

            check.ClearDomainEvents();
        }
    }
}
