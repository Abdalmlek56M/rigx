namespace HardwareStore.Common.Domain;

/// <summary>
/// Base type for every expected business failure (validation, not found, conflict, forbidden).
/// The API maps it once, globally, to a ProblemDetails response with <see cref="StatusCode"/>,
/// so endpoints never need their own try/catch for business errors.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    /// <summary>HTTP status to return (400, 403, 404, 409, 422 ...).</summary>
    public int StatusCode { get; }

    /// <summary>Field-level validation errors; when present the response is a validation problem.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
