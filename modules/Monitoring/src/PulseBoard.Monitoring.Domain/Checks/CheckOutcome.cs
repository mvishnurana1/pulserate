namespace PulseBoard.Monitoring.Domain.Checks;

/// <summary>
/// What happened when an endpoint was called once. Produced by whatever runs the HTTP request.
/// </summary>
public sealed record CheckOutcome
{
    private CheckOutcome(bool isSuccess, int? statusCode, TimeSpan responseTime, string? error)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        ResponseTime = responseTime;
        Error = error;
    }

    public bool IsSuccess { get; }

    /// <summary>HTTP status code, or null if no response came back (timeout, DNS failure).</summary>
    public int? StatusCode { get; }

    public TimeSpan ResponseTime { get; }

    public string? Error { get; }

    public static CheckOutcome Success(int statusCode, TimeSpan responseTime) =>
        new(isSuccess: true, statusCode, responseTime, error: null);

    public static CheckOutcome Failure(string error, TimeSpan responseTime, int? statusCode = null) =>
        new(isSuccess: false, statusCode, responseTime, error);
}
