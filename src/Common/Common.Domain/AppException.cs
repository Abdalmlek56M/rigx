namespace HardwareStore.Common.Domain;

/// <summary>
/// Base type for every expected business failure (validation, not found, conflict, forbidden).
/// The API maps it once, globally, to a ProblemDetails response with <see cref="StatusCode"/>,
/// so endpoints never need their own try/catch for business errors.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(int statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null,
        IReadOnlyDictionary<string, object?>? extensions = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
        Extensions = extensions;
    }

    /// <summary>HTTP status to return (400, 403, 404, 409, 422 ...).</summary>
    public int StatusCode { get; }

    /// <summary>Field-level validation errors; when present the response is a validation problem.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    /// <summary>Extra machine-readable members added to the ProblemDetails body (for example <c>attemptsLeft</c>).</summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; }
}

/// <summary>The request needs a signed-in user (401). Thrown when an endpoint reads the caller and there is none.</summary>
public sealed class UnauthenticatedException(string message = "Sign in first.", IReadOnlyDictionary<string, object?>? extensions = null)
    : AppException(401, message, extensions: extensions);
