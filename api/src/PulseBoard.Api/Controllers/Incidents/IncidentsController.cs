using Microsoft.AspNetCore.Mvc;
using PulseBoard.Api.Common;
using PulseBoard.Incidents.Application.Incidents;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Api.Controllers.Incidents;

/// <summary>
/// Incidents are opened and resolved automatically from check results. People can also resolve one by hand.
/// </summary>
[Route("api/incidents")]
public sealed class IncidentsController : ApiControllerBase
{
    /// <param name="status">"open" or "resolved". Leave empty for all incidents.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<IncidentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<IncidentResponse>>> GetAll(
        [FromServices] IQueryHandler<GetIncidentsQuery, Result<IReadOnlyList<IncidentResponse>>> handler,
        [FromQuery] string? status = null,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new GetIncidentsQuery(status, take), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetIncidentByIdQuery, Result<IncidentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetIncidentByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    [HttpPost("{id:guid}/resolve")]
    [ProducesResponseType<IncidentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidentResponse>> Resolve(
        Guid id,
        [FromServices] ICommandHandler<ResolveIncidentCommand, Result<IncidentResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResolveIncidentCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }
}
