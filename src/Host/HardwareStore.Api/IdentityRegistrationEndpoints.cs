using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

public static class IdentityRegistrationEndpoints
{
    public static IEndpointRouteBuilder MapIdentityRegistrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/registrations", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.Registration);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        CustomerRegistrationRequest request,
        HttpContext context,
        CustomerRegistrationService registrationService,
        ISecurityAuditWriter auditWriter,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        var result = await registrationService.RegisterAsync(request, cancellationToken);
        if (result.Status == CustomerRegistrationStatus.InvalidInput)
        {
            var field = result.InvalidField ?? "request";
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [field] = ["The supplied value is invalid."]
            });
        }

        var created = result.Status == CustomerRegistrationStatus.Created;
        await TryWriteAuditAsync(auditWriter, loggerFactory,
            new SecurityAuditEntry(
                created ? "Identity.Registration.Created" : "Identity.Registration.Rejected",
                created ? "Succeeded" : "Rejected",
                "Anonymous",
                SubjectUserAccountId: created ? result.UserAccountId : null,
                CorrelationId: context.TraceIdentifier,
                SafeMetadata: JsonSerializer.Serialize(new
                {
                    reason = created ? "Accepted" : "IdentifierConflict",
                    emailDispatchQueued = created && result.EmailDispatchQueued
                })), cancellationToken);

        if (created && !result.EmailDispatchQueued)
        {
            await TryWriteAuditAsync(auditWriter, loggerFactory,
                new SecurityAuditEntry(
                    "Identity.EmailVerification.DeliveryFailed",
                    "Failed",
                    "System",
                    SubjectUserAccountId: result.UserAccountId,
                    CorrelationId: context.TraceIdentifier,
                    SafeMetadata: "{\"reason\":\"DispatchUnavailable\"}"), cancellationToken);
        }

        return Results.Accepted(value: new
        {
            message = "If the registration can be completed, confirmation instructions will be sent."
        });
    }

    private static async Task TryWriteAuditAsync(
        ISecurityAuditWriter auditWriter,
        ILoggerFactory loggerFactory,
        SecurityAuditEntry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditWriter.WriteAsync(entry, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            loggerFactory.CreateLogger("Identity.Registration")
                .LogError("Unable to persist a registration security audit event.");
        }
    }
}
