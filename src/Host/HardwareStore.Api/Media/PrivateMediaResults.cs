using HardwareStore.Modules.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>Streams a private image (request photo, expense receipt) to a signed-in staff member: never cached, never public.</summary>
public static class PrivateMediaResults
{
    public static FileStreamHttpResult Image(HttpContext context, ICatalogMediaStorage storage, string storageKey)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        if (!storage.Exists(storageKey))
            throw new ApiProblemException(StatusCodes.Status404NotFound, "The image was not found.");
        return TypedResults.File(storage.OpenRead(storageKey), "image/webp");
    }
}
