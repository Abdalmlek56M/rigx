using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>Step-up confirmation for destructive actions: request a code, then send it in <c>X-Step-Up-Code</c>.</summary>
public static class StepUpEndpoints
{
    public static IEndpointRouteBuilder MapStepUpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/step-up/request", RequestAsync)
            .RequireAuthorization()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailCodeRequest);
        return endpoints;
    }

    /// <summary>
    /// Marks an endpoint as destructive: it runs only when the request carries a fresh email code.
    /// Use it for data reset, purge, customer-data export and security changes.
    /// </summary>
    public static TBuilder RequireStepUp<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(StepUpRequired.Instance).AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            await context.RequestServices.GetRequiredService<StepUpService>().ConfirmAsync(
                context.RequireUserId(), context.Request.Headers[StepUpService.HeaderName].ToString(), context.RequestAborted);
            return await next(invocation);
        });

    private static async Task<Accepted<EmailCodeTicketView>> RequestAsync(
        HttpContext context,
        StepUpService stepUp,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        var ticket = await stepUp.RequestAsync(accountId, cancellationToken);
        return TypedResults.Accepted((string?)null, EmailCodeTicketView.From(ticket, timeProvider));
    }
}

/// <summary>Endpoint metadata: the endpoint needs the <c>X-Step-Up-Code</c> header (shown in the OpenAPI document).</summary>
public sealed class StepUpRequired
{
    public static readonly StepUpRequired Instance = new();

    private StepUpRequired()
    {
    }
}
