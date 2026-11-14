using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace HardwareStore.Modules.Identity.Authentication;

public sealed class CsrfProtectionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        try
        {
            if (!await antiforgery.IsRequestValidAsync(context))
            {
                await RejectAsync(context);
                return;
            }
        }
        catch (AntiforgeryValidationException)
        {
            await RejectAsync(context);
            return;
        }

        await next(context);
    }

    private static Task RejectAsync(HttpContext context) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid anti-forgery token.",
            instance: context.Request.Path).ExecuteAsync(context);
}
