using Microsoft.AspNetCore.Mvc;
using PulseBoard.SharedKernel;

namespace PulseBoard.Api.Common;

// No [Produces] attribute: it would force problem responses to application/json instead of
// application/problem+json (RFC 9457). Successful responses are JSON by default anyway.
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Turns an expected failure from a handler into an RFC 9457 problem response,
    /// with the error's code included so clients can react to specific failures.
    /// </summary>
    protected ObjectResult ErrorResult(Error error)
    {
        var (statusCode, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode, title, detail: error.Description);
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
