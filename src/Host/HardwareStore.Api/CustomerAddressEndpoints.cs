using HardwareStore.Modules.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static class CustomerAddressEndpoints
{
    public static IEndpointRouteBuilder MapCustomerAddressEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity/addresses");
        group.MapGet("", ListAsync).RequireAuthorization();
        group.MapPost("", CreateAsync).RequireAuthorization();
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization();
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<CustomerAddressView>>> ListAsync(
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var accountId = context.RequireUserId();
        return TypedResults.Ok(await addresses.ListAsync(accountId, cancellationToken));
    }

    private static async Task<Ok<CustomerAddressView>> CreateAsync(
        CustomerAddressCreateRequest request,
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        return TypedResults.Ok(await addresses.CreateAsync(accountId, request, cancellationToken));
    }

    private static async Task<Ok<CustomerAddressView>> UpdateAsync(
        Guid id,
        CustomerAddressUpdateRequest request,
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        return TypedResults.Ok(await addresses.UpdateAsync(accountId, id, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteAsync(
        Guid id,
        long rowVersion,
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await addresses.DeleteAsync(accountId, id, rowVersion, cancellationToken);
        return TypedResults.NoContent();
    }
}
