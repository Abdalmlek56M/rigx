using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.PcBuilder;
using Microsoft.AspNetCore.Http;

public static class PcBuilderEndpoints
{
    public static IEndpointRouteBuilder MapPcBuilderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/pc-builder/templates", ListPublicAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/pc-builder/templates/{id:guid}", GetPublicAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/pc-builder/templates/{id:guid}/evaluate", EvaluateAsync).AllowAnonymous();

        var admin = endpoints.MapGroup("/api/v1/admin/pc-builder/templates");
        admin.MapGet("", ListAdminAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("", CreateAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/{id:guid}", UpdateAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/{id:guid}/publish", PublishAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync).RequirePermission("Catalog.ManageProducts");
        return endpoints;
    }

    private static async Task<IResult> ListPublicAsync(
        HttpContext context, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.ListPublicAsync(cancellationToken));
    }

    private static async Task<IResult> GetPublicAsync(
        Guid id, HttpContext context, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        try { return Results.Ok(await service.GetPublicAsync(id, cancellationToken)); }
        catch (PcBuildException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> EvaluateAsync(
        Guid id, PcBuildCompatibilityRequest request, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await service.EvaluateAsync(id, request, cancellationToken)); }
        catch (PcBuildException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListAdminAsync(
        HttpContext context, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await service.ListAdminAsync(cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        PcBuildInput request, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await service.CreateAsync(request, cancellationToken)); }
        catch (PcBuildException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, PcBuildUpdateRequest request, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await service.UpdateAsync(id, request, cancellationToken)); }
        catch (PcBuildException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> PublishAsync(
        Guid id, PcBuildVersionRequest request, PcBuildTemplateService service, CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, true, request, service, cancellationToken);

    private static async Task<IResult> UnpublishAsync(
        Guid id, PcBuildVersionRequest request, PcBuildTemplateService service, CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, false, request, service, cancellationToken);

    private static async Task<IResult> SetPublishedAsync(
        Guid id, bool published, PcBuildVersionRequest request, PcBuildTemplateService service, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await service.SetPublishedAsync(id, published, request.RowVersion, cancellationToken)); }
        catch (PcBuildException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
}

public sealed record PcBuildVersionRequest(long RowVersion);
