using HardwareStore.Modules.Identity.Authentication;

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
        builder.AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            if (!context.TryGetUserId(out var accountId))
                return Results.Unauthorized();
            await context.RequestServices.GetRequiredService<StepUpService>().ConfirmAsync(
                accountId, context.Request.Headers[StepUpService.HeaderName].ToString(), context.RequestAborted);
            return await next(invocation);
        });

    private static async Task<IResult> RequestAsync(
        HttpContext context,
        StepUpService stepUp,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        var ticket = await stepUp.RequestAsync(accountId, cancellationToken);
        return Results.Accepted(value: EmailCodeTicketView.From(ticket, timeProvider));
    }
}
