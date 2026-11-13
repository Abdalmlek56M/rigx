using System.Text.Json;
using System.Threading.RateLimiting;
using HardwareStore.Modules.Auditing;
using Microsoft.AspNetCore.RateLimiting;

public static class IdentityRateLimitPolicies
{
    public const string Login = "identity-login";
    public const string Registration = "identity-registration";
    public const string EmailVerification = "identity-email-verification";
    public const string EmailVerificationResend = "identity-email-verification-resend";
    public const string PasswordResetRequest = "identity-password-reset-request";
    public const string PasswordReset = "identity-password-reset";
    public const string EmailChange = "identity-email-change";
    public const string EmailOtpRequest = "identity-email-otp-request";
    public const string EmailOtpVerify = "identity-email-otp-verify";
    public const string Contact = "contact-submit";
    public const string CatalogSuggest = "catalog-suggest";
}

public static class IdentityRateLimiting
{
    public static IServiceCollection AddIdentityRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.Login, 5, TimeSpan.FromMinutes(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.Registration, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailVerification, 20, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailVerificationResend, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.PasswordResetRequest, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.PasswordReset, 20, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailChange, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailOtpRequest, 10, TimeSpan.FromHours(1));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.EmailOtpVerify, 20, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.Contact, 5, TimeSpan.FromMinutes(10));
            AddFixedWindowPolicy(options, IdentityRateLimitPolicies.CatalogSuggest, 120, TimeSpan.FromMinutes(1));

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
        IdentityRateLimitPolicies.Login => "60",
        IdentityRateLimitPolicies.EmailVerification => "600",
        IdentityRateLimitPolicies.PasswordReset => "600",
        IdentityRateLimitPolicies.EmailChange => "3600",
        _ => "3600"
    };

    private static string EventType(string policyName) => policyName switch
    {
        IdentityRateLimitPolicies.Login => "Identity.Login.RateLimited",
        IdentityRateLimitPolicies.Registration => "Identity.Registration.RateLimited",
        IdentityRateLimitPolicies.EmailVerification => "Identity.EmailVerification.RateLimited",
        IdentityRateLimitPolicies.EmailVerificationResend => "Identity.EmailVerification.Resend.RateLimited",
        IdentityRateLimitPolicies.PasswordResetRequest => "Identity.PasswordReset.Request.RateLimited",
        IdentityRateLimitPolicies.PasswordReset => "Identity.PasswordReset.RateLimited",
        IdentityRateLimitPolicies.EmailChange => "Identity.EmailChange.RateLimited",
        IdentityRateLimitPolicies.EmailOtpRequest => "Identity.EmailOtp.Request.RateLimited",
        IdentityRateLimitPolicies.EmailOtpVerify => "Identity.EmailOtp.Verify.RateLimited",
        IdentityRateLimitPolicies.Contact => "Contact.Submit.RateLimited",
        IdentityRateLimitPolicies.CatalogSuggest => "Catalog.Search.RateLimited",
        _ => "Identity.RateLimited"
    };
}
