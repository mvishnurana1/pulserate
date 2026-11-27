using Microsoft.EntityFrameworkCore;
using PulseBoard.Incidents.Application.Abstractions;
using PulseBoard.Incidents.Domain.Incidents;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Incidents.Application.Incidents;

/// <param name="Status">"open", "resolved", or null for all.</param>
public sealed record GetIncidentsQuery(string? Status, int Take = 100)
{
    public const int MaxTake = 500;
}

public sealed record GetIncidentByIdQuery(Guid Id);

public sealed record ResolveIncidentCommand(Guid Id);

internal sealed class GetIncidentsHandler(IIncidentsDbContext db)
    : IQueryHandler<GetIncidentsQuery, Result<IReadOnlyList<IncidentResponse>>>
{
    public async Task<Result<IReadOnlyList<IncidentResponse>>> HandleAsync(
        GetIncidentsQuery query,
        CancellationToken cancellationToken)
    {
        IncidentStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<IncidentStatus>(query.Status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            {
                return IncidentErrors.InvalidStatusFilter;
            }

            status = parsed;
        }

        var incidents = db.Incidents.AsNoTracking();
        if (status is not null)
        {
            incidents = incidents.Where(i => i.Status == status);
        }

        var results = await incidents
            .OrderByDescending(i => i.OpenedAt)
            .Take(Math.Clamp(query.Take, 1, GetIncidentsQuery.MaxTake))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<IncidentResponse>>(results.Select(IncidentResponse.From).ToList());
    }
}

internal sealed class GetIncidentByIdHandler(IIncidentsDbContext db)
    : IQueryHandler<GetIncidentByIdQuery, Result<IncidentResponse>>
{
    public async Task<Result<IncidentResponse>> HandleAsync(GetIncidentByIdQuery query, CancellationToken cancellationToken)
    {
        var incident = await db.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.Id == query.Id, cancellationToken);
        return incident is null ? IncidentErrors.NotFound(query.Id) : IncidentResponse.From(incident);
    }
}

internal sealed class ResolveIncidentHandler(IIncidentsDbContext db, TimeProvider clock)
    : ICommandHandler<ResolveIncidentCommand, Result<IncidentResponse>>
{
    public async Task<Result<IncidentResponse>> HandleAsync(ResolveIncidentCommand command, CancellationToken cancellationToken)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken);
        if (incident is null)
        {
            return IncidentErrors.NotFound(command.Id);
        }

        var resolved = incident.Resolve(IncidentResolution.Manual, clock.GetUtcNow());
        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return IncidentResponse.From(incident);
    }
}
