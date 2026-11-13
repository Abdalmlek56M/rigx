using HardwareStore.Modules.Identity;
using Microsoft.AspNetCore.Http;

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

    private static async Task<IResult> ListAsync(
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return context.TryGetUserId(out var accountId)
            ? Results.Ok(await addresses.ListAsync(accountId, cancellationToken))
            : Results.Unauthorized();
    }

    private static async Task<IResult> CreateAsync(
        CustomerAddressCreateRequest request,
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await addresses.CreateAsync(accountId, request, cancellationToken));
        }
        catch (IdentityAdministrationException exception)
        {
            return Map(exception);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        CustomerAddressUpdateRequest request,
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await addresses.UpdateAsync(accountId, id, request, cancellationToken));
        }
        catch (IdentityAdministrationException exception)
        {
            return Map(exception);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        long rowVersion,
        HttpContext context,
        CustomerAddressService addresses,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            await addresses.DeleteAsync(accountId, id, rowVersion, cancellationToken);
            return Results.NoContent();
        }
        catch (IdentityAdministrationException exception)
        {
            return Map(exception);
        }
    }

    private static IResult Map(IdentityAdministrationException exception) =>
        Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
}
