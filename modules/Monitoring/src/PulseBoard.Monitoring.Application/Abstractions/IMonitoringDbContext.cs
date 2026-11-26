using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Domain.Checks;

namespace PulseBoard.Monitoring.Application.Abstractions;

/// <summary>
/// The Monitoring module's unit of work. Handlers query EF Core directly instead of going
/// through repositories; see docs/adr/0002-ef-core-in-application-layer.md for why.
/// </summary>
public interface IMonitoringDbContext
{
    DbSet<Check> Checks { get; }

    DbSet<CheckResult> CheckResults { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
