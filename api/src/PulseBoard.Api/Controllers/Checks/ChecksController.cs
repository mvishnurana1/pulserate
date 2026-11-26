using Microsoft.AspNetCore.Mvc;
using PulseBoard.Api.Common;
using PulseBoard.Monitoring.Application.Checks;
using PulseBoard.SharedKernel;
using PulseBoard.SharedKernel.Messaging;

namespace PulseBoard.Api.Controllers.Checks;

/// <summary>
/// Manage the endpoints PulseBoard monitors. Each action injects only the handler it needs,
/// so controllers stay thin and handlers are easy to test on their own.
/// </summary>
[Route("api/checks")]
public sealed class ChecksController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CheckResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckResponse>>> GetAll(
        [FromServices] IQueryHandler<GetChecksQuery, IReadOnlyList<CheckResponse>> handler,
        CancellationToken cancellationToken)
    {
        var checks = await handler.HandleAsync(new GetChecksQuery(), cancellationToken);
        return Ok(checks);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckResponse>> GetById(
        Guid id,
        [FromServices] IQueryHandler<GetCheckByIdQuery, Result<CheckResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCheckByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    [HttpGet("{id:guid}/results")]
    [ProducesResponseType<IReadOnlyList<CheckResultResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CheckResultResponse>>> GetResults(
        Guid id,
        [FromServices] IQueryHandler<GetCheckResultsQuery, Result<IReadOnlyList<CheckResultResponse>>> handler,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new GetCheckResultsQuery(id, take), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    [HttpPost]
    [ProducesResponseType<CheckResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CheckResponse>> Create(
        CreateCheckRequest request,
        [FromServices] ICommandHandler<CreateCheckCommand, Result<CheckResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : ErrorResult(result.Error);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<CheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckResponse>> Update(
        Guid id,
        UpdateCheckRequest request,
        [FromServices] ICommandHandler<UpdateCheckCommand, Result<CheckResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] ICommandHandler<DeleteCheckCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteCheckCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : ErrorResult(result.Error);
    }

    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType<CheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckResponse>> Pause(
        Guid id,
        [FromServices] ICommandHandler<PauseCheckCommand, Result<CheckResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PauseCheckCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }

    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType<CheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckResponse>> Resume(
        Guid id,
        [FromServices] ICommandHandler<ResumeCheckCommand, Result<CheckResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResumeCheckCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ErrorResult(result.Error);
    }
}
