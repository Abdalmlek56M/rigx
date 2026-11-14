using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// The backups page: history, run now and restore check (<c>System.ManageBackups</c>); download of the archive, which holds all customer
/// data, is owner-only (<c>System.BackupDownload</c>) with a fresh code.
/// </summary>
public static class BackupEndpoints
{
    private const string Manage = "System.ManageBackups";
    private const string Download = "System.BackupDownload";

    public static IEndpointRouteBuilder MapBackupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/system/backups");
        group.MapGet("", ListAsync).RequirePermission(Manage);
        group.MapPost("", RunNowAsync).RequirePermission(Manage);
        group.MapPost("/{id:guid}/verify", VerifyAsync).RequirePermission(Manage);
        group.MapGet("/{id:guid}/download", DownloadAsync).RequirePermission(Download).RequireStepUp()
            .Produces<Stream>(StatusCodes.Status200OK, "application/zip");
        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<BackupRun>>> ListAsync(HttpContext context, BackupService backups, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await backups.ListAsync(cancellationToken));
    }

    private static async Task<Ok<BackupRun>> RunNowAsync(HttpContext context, BackupService backups, CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await backups.RunAsync(BackupTrigger.Manual, actorId, cancellationToken));
    }

    private static async Task<Ok<BackupRun>> VerifyAsync(Guid id, HttpContext context, BackupService backups, CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await backups.VerifyAsync(id, actorId, cancellationToken));
    }

    private static async Task<FileStreamHttpResult> DownloadAsync(Guid id, HttpContext context, BackupService backups, CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        var (content, fileName) = await backups.OpenDownloadAsync(id, actorId, cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.File(content, "application/zip", fileName);
    }
}
