using HardwareStore.Common.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentry;

public sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AppException appException)
        {
            // Expected business failure: not logged as an error, mapped the same way for every module.
            httpContext.Response.StatusCode = appException.StatusCode;
            var problem = appException.Errors is { Count: > 0 } errors
                ? new HttpValidationProblemDetails(errors.ToDictionary(pair => pair.Key, pair => pair.Value))
                : new ProblemDetails();
            problem.Status = appException.StatusCode;
            problem.Title = appException.Message;
            problem.Instance = httpContext.Request.Path;
            foreach (var (key, value) in appException.Extensions ?? new Dictionary<string, object?>())
                problem.Extensions[key] = value;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }

        if (exception is DbUpdateConcurrencyException)
        {
            logger.LogWarning(exception, "Concurrent update rejected for {Path}.", httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "The record was changed by another user. Reload and try again.",
                    Instance = httpContext.Request.Path
                },
                Exception = exception
            });
        }

        logger.LogError(exception, "Unhandled request exception.");
        // .NET 10 does not report exceptions an IExceptionHandler handled, so unexpected ones go to Sentry here (no-op without a DSN).
        SentrySdk.CaptureException(exception);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Instance = httpContext.Request.Path
            },
            Exception = exception
        });
    }
}
