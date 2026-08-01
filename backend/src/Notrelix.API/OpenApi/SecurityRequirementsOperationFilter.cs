using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notrelix.API.OpenApi;

/// <summary>
/// Applies Bearer security requirement per-operation based on endpoint metadata.
/// Anonymous operations (with [AllowAnonymous] or IAnonymousRequest metadata) get no security.
/// All other operations require Bearer authentication.
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous = context.MethodInfo is not null
            && context.MethodInfo.GetCustomAttributes(true)
                .OfType<IAllowAnonymous>()
                .Any();

        if (hasAllowAnonymous)
        {
            operation.Security = [];
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            }
        ];
    }
}
