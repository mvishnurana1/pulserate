using Microsoft.EntityFrameworkCore;
using PulseBoard.Monitoring.Application.Abstractions;
using PulseBoard.Monitoring.Domain.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Monitoring.Application.Checks;

public sealed record GetChecksQuery;

public sealed record GetCheckByIdQuery(Guid Id);

public sealed record GetCheckResultsQuery(Guid CheckId, int Take = 50)
{
    public const int MaxTake = 500;
}

internal sealed class GetChecksHandler(IMonitoringDbContext db)
    : IQueryHandler<GetChecksQuery, IReadOnlyList<CheckResponse>>
{
    public async Task<IReadOnlyList<CheckResponse>> HandleAsync(GetChecksQuery query, CancellationToken cancellationToken)
    {
        var checks = await db.Checks
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return checks.Select(CheckResponse.From).ToList();
    }
}

internal sealed class GetCheckByIdHandler(IMonitoringDbContext db)
    : IQueryHandler<GetCheckByIdQuery, Result<CheckResponse>>
{
    public async Task<Result<CheckResponse>> HandleAsync(GetCheckByIdQuery query, CancellationToken cancellationToken)
    {
        var check = await db.Checks
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken);

        return check is null ? CheckErrors.NotFound(query.Id) : CheckResponse.From(check);
    }
}

internal sealed class GetCheckResultsHandler(IMonitoringDbContext db)
    : IQueryHandler<GetCheckResultsQuery, Result<IReadOnlyList<CheckResultResponse>>>
{
    public async Task<Result<IReadOnlyList<CheckResultResponse>>> HandleAsync(
        GetCheckResultsQuery query,
        CancellationToken cancellationToken)
    {
        if (!await db.Checks.AnyAsync(c => c.Id == query.CheckId, cancellationToken))
        {
            return CheckErrors.NotFound(query.CheckId);
        }

        var results = await db.CheckResults
            .AsNoTracking()
            .Where(r => r.CheckId == query.CheckId)
            .OrderByDescending(r => r.CheckedAt)
            .Take(Math.Clamp(query.Take, 1, GetCheckResultsQuery.MaxTake))
            .Select(r => new CheckResultResponse(r.Id, r.CheckedAt, r.IsSuccess, r.StatusCode, r.ResponseTimeMs, r.Error))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CheckResultResponse>>(results);
    }
}
