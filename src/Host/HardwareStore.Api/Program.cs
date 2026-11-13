using System.Net;
using HardwareStore.Common.Infrastructure.Persistence;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Inventory;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using HardwareStore.Modules.Auditing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, _, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddValidation();
var dataProtectionKeyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
{
    var keyRingDirectory = new DirectoryInfo(dataProtectionKeyRingPath);
    keyRingDirectory.Create();
    builder.Services.AddDataProtection()
        .SetApplicationName("HardwareStore")
        .PersistKeysToFileSystem(keyRingDirectory);
}
builder.Services.AddPermissionAuthorization();
builder.Services.AddIdentityPersistence(builder.Configuration);
builder.Services.AddIdentityAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddIdentityAccounts(builder.Environment, builder.Configuration);
builder.Services.AddCatalogModule(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddPurchasingModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddStorefrontModule(builder.Configuration);
builder.Services.AddPcBuilderModule(builder.Configuration);
builder.Services.AddRetailModule(builder.Configuration);
builder.Services.AddSingleton(OrderLinks.FromConfiguration(builder.Configuration));
builder.Services.AddHostedService<ReservationExpiryJob>();
builder.Services.AddScoped<HardwareStore.Modules.Catalog.IMediaReferenceGuard, RetailMediaReferenceGuard>();
builder.Services.AddSecurityAuditPersistence(builder.Configuration);
var previewEnabled = builder.Environment.IsDevelopment() &&
                     builder.Configuration.GetValue("Preview:Enabled", false);
if (previewEnabled)
{
    builder.Services.AddSingleton(new PreviewOptions
    {
        Enabled = true,
        BootstrapKey = builder.Configuration["Preview:BootstrapKey"] ?? string.Empty,
        OwnerEmail = builder.Configuration["Preview:OwnerEmail"] ?? string.Empty,
        OwnerUsername = builder.Configuration["Preview:OwnerUsername"] ?? string.Empty,
        OwnerFullName = builder.Configuration["Preview:OwnerFullName"] ?? string.Empty,
        MigrationConnectionString = builder.Configuration.GetConnectionString("Migrations")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Migrations is required when Preview is enabled."),
        RuntimeConnectionString = builder.Configuration.GetConnectionString("Default") ?? string.Empty
    });
    builder.Services.AddScoped<PreviewInitializationService>();
}
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);

    foreach (var address in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        if (!IPAddress.TryParse(address, out var proxyAddress))
            throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contains an invalid IP address: '{address}'.");
        options.KnownProxies.Add(proxyAddress);
    }
});
builder.Services.AddIdentityRateLimiting();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

builder.Services.AddDbContext<HardwareStoreDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<AdminAnalyticsService>();
builder.Services.AddScoped<PurchaseReceiptService>();
builder.Services.AddScoped<RequestPurchaseConversion>();
builder.Services.AddHealthChecks()
    .AddCheck("live", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

if (args.Contains("--migrate", StringComparer.Ordinal))
{
    Environment.ExitCode = await DatabaseMigrator.RunCommandAsync(app.Services);
    return;
}

if (args.Contains("--bootstrap-owner", StringComparer.Ordinal))
{
    Environment.ExitCode = await OwnerBootstrapCommand.RunAsync(app.Services);
    return;
}

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthorization();

app.MapGet("/api/v1/identity/antiforgery", (HttpContext context, Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.Pragma = "no-cache";
    return Results.Ok(new { requestToken = tokens.RequestToken });
}).AllowAnonymous();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.MapEmailCodeSignInEndpoints();
app.MapGoogleSignInEndpoints();
app.MapStepUpEndpoints();
app.MapRequestEndpoints();
app.MapArticleVideoEndpoints();
app.MapIdentityCurrentUserEndpoints();
app.MapIdentitySessionEndpoints();
app.MapProfileAvatarEndpoints();
app.MapAccountGovernanceEndpoints();
app.MapCustomerAddressEndpoints();
app.MapAuditEndpoints();
app.MapContentEndpoints();
app.MapOfferEndpoints();
app.MapContentMediaEndpoints();
app.MapContentVideoEndpoints();
app.MapReviewEndpoints();
app.MapAdminAnalyticsEndpoints();
app.MapPcBuilderEndpoints();
app.MapRetailEndpoints();
app.MapIdentityAdministrationEndpoints();
app.MapCatalogEndpoints();
app.MapPurchasingEndpoints();
app.MapInventoryEndpoints();
app.MapStorefrontEndpoints();
if (previewEnabled)
    app.MapPreviewInitializationEndpoints();

app.Run();

public partial class Program
{
}
