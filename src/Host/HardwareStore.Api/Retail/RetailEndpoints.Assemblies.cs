using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListAssembliesAsync(HttpContext context, AssemblyService assemblies, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await assemblies.ListAssembliesAsync(HasPermission(context, "Inventory.ViewCosts"), ct));
    }

    private static async Task<IResult> ListPublishedAssembliesAsync(HttpContext context, AssemblyService assemblies, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await assemblies.ListPublishedAssembliesAsync(ct));
    }

    private static async Task<IResult> CreateAssemblyAsync(AssemblyCreateRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.CreateAssemblyAsync(request, ct));

    private static async Task<IResult> UpdateAssemblyAsync(Guid id, AssemblyUpdateRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.UpdateAssemblyAsync(id, request, ct));

    private static async Task<IResult> SetAllocationsAsync(Guid id, AssemblyAllocationRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SetAssemblyAllocationsAsync(id, request, ct));

    private static async Task<IResult> PublishAssemblyAsync(Guid id, RetailVersionRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SetAssemblyPublishedAsync(id, true, request.RowVersion, ct));

    private static async Task<IResult> UnpublishAssemblyAsync(Guid id, RetailVersionRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SetAssemblyPublishedAsync(id, false, request.RowVersion, ct));

    private static async Task<IResult> RemoveUnitAsync(Guid id, Guid unitId, AssemblyService assemblies, CancellationToken ct, long rowVersion = 0) =>
        Results.Ok(await assemblies.RemoveUnitAsync(id, unitId, rowVersion, ct));

    private static async Task<IResult> DisassembleAsync(Guid id, RetailVersionRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.DisassembleAsync(id, request.RowVersion, ct));

    private static async Task<IResult> SwapAsync(SwapRequest request, AssemblyService assemblies, CancellationToken ct) =>
        Results.Ok(await assemblies.SwapUnitAsync(request, ct));
}
