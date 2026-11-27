using Microsoft.EntityFrameworkCore;
using PulseBoard.Incidents.Domain.Incidents;

namespace PulseBoard.Incidents.Application.Abstractions;

public interface IIncidentsDbContext
{
    DbSet<Incident> Incidents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
