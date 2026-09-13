using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SPOCS.API.Filters;

/// <summary>
/// Adds the Bearer security requirement to any operation that requires authorization.
/// This ensures Swagger UI actually sends the Authorization header for protected endpoints.
/// </summary>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Check if the controller or action has [Authorize]
        var hasAuthorize =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true).OfType<AuthorizeAttribute>().Any() == true
            || context.MethodInfo.GetCustomAttributes(inherit: true).OfType<AuthorizeAttribute>().Any();

        // [AllowAnonymous] overrides [Authorize]
        var hasAllowAnonymous =
            context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true).OfType<AllowAnonymousAttribute>().Any() == true
            || context.MethodInfo.GetCustomAttributes(inherit: true).OfType<AllowAnonymousAttribute>().Any();

        if (!hasAuthorize || hasAllowAnonymous)
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    }
}
