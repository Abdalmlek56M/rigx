using HardwareStore.Modules.Catalog;
using Microsoft.AspNetCore.Http;

/// <summary>Streams a private image (request photo, expense receipt) to a signed-in staff member: never cached, never public.</summary>
public static class PrivateMediaResults
{
    public static IResult Image(HttpContext context, ICatalogMediaStorage storage, string storageKey)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return storage.Exists(storageKey) ? Results.File(storage.OpenRead(storageKey), "image/webp") : Results.NotFound();
    }
}
