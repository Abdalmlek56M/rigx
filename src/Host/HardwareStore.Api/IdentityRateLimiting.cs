using System.Text.Json;
using System.Threading.RateLimiting;
using HardwareStore.Modules.Auditing;
using Microsoft.AspNetCore.RateLimiting;

public static class IdentityRateLimitPolicies
{
    public const string EmailChange = "identity-email-change";
    public const string EmailChangeConfirm = "identity-email-change-confirm";
    public const string EmailCodeRequest = "identity-email-code-request";
    public const string EmailCodeVerify = "identity-email-code-verify";
    public const string Contact = "contact-submit";
    public const string CatalogSuggest = "catalog-suggest";
    public const string Checkout = "order-checkout";
    public const string OrderTracking = "order-tracking";
    public const string WarrantyLookup = "warranty-lookup";
}

public static class IdentityRateLimiting
{
    public static IServiceCollection AddIdentityRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailChange, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailChangeConfirm, 20, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailCodeRequest, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailCodeVerify, 20, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.Contact, 5, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.CatalogSuggest, 120, TimeSpan.FromMinutes(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.Checkout, 10, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.OrderTracking, 60, TimeSpan.FromMinutes(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.WarrantyLookup, 20, TimeSpan.FromMinutes(10));

            options.OnRejected = async (rejection, cancellationToken) =>
            {
                var context = rejection.HttpContext;
                var policyName = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                    ?? "identity";
                context.Response.Headers.RetryAfter = RetryAfterSeconds(policyName);

                var auditWriter = context.RequestServices.GetRequiredService<ISecurityAuditWriter>();
                try
                {
                    await auditWriter.WriteAsync(new SecurityAuditEntry(
                        EventType(policyName),
                        "Rejected",
                        "Anonymous",
                        CorrelationId: context.TraceIdentifier,
                        SafeMetadata: JsonSerializer.Serialize(new { reason = "IpRateLimit" })),
                        cancellationToken);
                }
                catch (Exception exception)
                {
                    context.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Identity.RateLimiting")
                        .LogError(exception, "Unable to persist rate-limit audit event.");
                }
            };
        });
        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        int permitLimit,
        TimeSpan window)
    {
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    private static string RetryAfterSeconds(string policyName) => policyName switch
    {
        IdentityRateLimitPolicies.EmailChangeConfirm or IdentityRateLimitPolicies.EmailCodeVerify => "600",
        IdentityRateLimitPolicies.CatalogSuggest or IdentityRateLimitPolicies.OrderTracking => "60",
        IdentityRateLimitPolicies.Checkout or IdentityRateLimitPolicies.WarrantyLookup => "600",
        _ => "3600"
    };

    private static string EventType(string policyName) => policyName switch
    {
        IdentityRateLimitPolicies.EmailChange => "Identity.EmailChange.RateLimited",
        IdentityRateLimitPolicies.EmailChangeConfirm => "Identity.EmailChange.Confirm.RateLimited",
        IdentityRateLimitPolicies.EmailCodeRequest => "Identity.EmailCode.Request.RateLimited",
        IdentityRateLimitPolicies.EmailCodeVerify => "Identity.EmailCode.Verify.RateLimited",
        IdentityRateLimitPolicies.Contact => "Contact.Submit.RateLimited",
        IdentityRateLimitPolicies.CatalogSuggest => "Catalog.Search.RateLimited",
        IdentityRateLimitPolicies.Checkout => "Sales.Checkout.RateLimited",
        IdentityRateLimitPolicies.WarrantyLookup => "Sales.WarrantyLookup.RateLimited",
        IdentityRateLimitPolicies.OrderTracking => "Sales.Tracking.RateLimited",
        _ => "Identity.RateLimited"
    };
}
