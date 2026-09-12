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
        // Minimal API endpoint metadata (WhereMetadata/IAllowAnonymous applied
        // on the route builder) does not reflect on MethodInfo — it lives in
        // the action descriptor. Both surfaces must be consulted, otherwise a
        // signature-authenticated anonymous endpoint exports a phantom Bearer
        // requirement.
        var hasAllowAnonymous =
            (context.MethodInfo?.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any() ?? false)
            || context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

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
