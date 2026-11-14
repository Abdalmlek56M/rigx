using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;

/// <summary>
/// The API contract: one OpenAPI document (<c>/openapi/v1.json</c>, Development only) generated from the endpoints.
/// Adds the cookie + CSRF security schemes, the ProblemDetails error response on every operation, one tag per area,
/// and the permission / step-up each operation needs. The frontend TypeScript client is generated from this document.
/// </summary>
public static class OpenApiSetup
{
    public const string DocumentName = "v1";
    private const string CookieScheme = "sessionCookie";
    private const string CsrfScheme = "csrfHeader";
    private const string ProblemSchema = "ProblemDetails";

    public static IServiceCollection AddApiContract(this IServiceCollection services) =>
        services.AddOpenApi(DocumentName, options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "RIGX API",
                    Version = "v1",
                    Description = "Errors are RFC 9457 ProblemDetails (`title`, `status`; `errors` for field errors). " +
                                  "Lists are `{ items, page, pageSize, total }` with 1-based `page` and `pageSize` <= 100. " +
                                  "Unsafe methods need the `X-CSRF-TOKEN` header from `GET /api/v1/identity/antiforgery`."
                };
                document.Servers = [];
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes[CookieScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Name = "__Host-HardwareStore.Session",
                    Description = "HttpOnly session cookie set by email-code or Google sign-in."
                };
                document.Components.SecuritySchemes[CsrfScheme] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = "X-CSRF-TOKEN",
                    Description = "Anti-forgery token for POST/PUT/PATCH/DELETE."
                };
                return Task.CompletedTask;
            });
            // The server also reads numbers sent as strings, but always writes numbers: the contract says number only.
            options.AddSchemaTransformer((schema, _, _) =>
            {
                if (schema.Type is { } type && type.HasFlag(JsonSchemaType.String) &&
                    (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number)))
                {
                    schema.Type = type & ~JsonSchemaType.String;
                    schema.Pattern = null;
                }

                return Task.CompletedTask;
            });
            options.AddOperationTransformer(async (operation, context, cancellationToken) =>
            {
                var path = "/" + (context.Description.RelativePath ?? string.Empty);
                operation.Tags = new HashSet<OpenApiTagReference> { new(AreaTag(path), context.Document) };

                var problem = await context.GetOrCreateSchemaAsync(typeof(HttpValidationProblemDetails), null, cancellationToken);
                problem.AdditionalProperties = new OpenApiSchema(); // extensions such as attemptsLeft
                context.Document?.AddComponent(ProblemSchema, problem);
                operation.Responses ??= new OpenApiResponses();
                operation.Responses["default"] = new OpenApiResponse
                {
                    Description = "Error (ProblemDetails).",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/problem+json"] = new() { Schema = new OpenApiSchemaReference(ProblemSchema, context.Document) }
                    }
                };

                var metadata = context.Description.ActionDescriptor.EndpointMetadata;
                var notes = new List<string>();
                var anonymous = metadata.OfType<IAllowAnonymous>().Any();
                if (!anonymous && metadata.Any(item => item is IAuthorizeData or AuthorizationPolicy))
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference(CookieScheme, context.Document)] = []
                    });
                    var permissions = metadata.OfType<AuthorizationPolicy>()
                        .SelectMany(policy => policy.Requirements.OfType<PermissionRequirement>())
                        .Select(requirement => requirement.Permission)
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    notes.Add(permissions.Count > 0 ? $"Permission: `{string.Join("`, `", permissions)}`." : "Signed-in user.");
                }

                if (metadata.OfType<StepUpRequired>().Any())
                    notes.Add($"Step-up: send the emailed code in `{StepUpService.HeaderName}`.");
                if (notes.Count > 0)
                    operation.Description = string.Join(" ", new[] { operation.Description }.Concat(notes)
                        .Where(text => !string.IsNullOrWhiteSpace(text)));
            });
        });

    /// <summary>Tag = area: <c>/api/v1/admin/retail/...</c> → <c>admin/retail</c>, <c>/api/v1/store/...</c> → <c>store</c>.</summary>
    private static string AreaTag(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var rest = segments.Length > 2 && segments[0] == "api" ? segments[2..] : segments;
        if (rest.Length == 0) return "root";
        return rest[0] == "admin" && rest.Length > 1 ? $"admin/{rest[1]}" : rest[0];
    }
}
