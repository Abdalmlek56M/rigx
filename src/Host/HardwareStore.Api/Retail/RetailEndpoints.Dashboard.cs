using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private const int DefaultDashboardDays = 30;

    /// <summary>Default period: the last 30 days ending today in business time.</summary>
    private static async Task<IResult> GetDashboardAsync(HttpContext context, DashboardComposer composer, TimeZoneInfo businessTimeZone,
        TimeProvider timeProvider, CancellationToken ct, DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessTimeZone);
        return Results.Ok(await composer.BuildAsync(from ?? end.AddDays(1 - DefaultDashboardDays), end, businessTimeZone, ct));
    }
}
