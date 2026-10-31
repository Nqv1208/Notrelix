using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Identity.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notrelix.API.OpenApi;

/// <summary>
/// Describes selected enums as their member names. The HTTP JSON pipeline serializes every enum as a
/// string (<c>JsonStringEnumConverter</c>), but the minimal-API schema generator does not read that
/// option and would export the integer values, which no client ever receives. Opt-in on purpose: enums
/// that already shipped as integers in the checked-in contract are not changed here.
/// </summary>
public sealed class StringEnumSchemaFilter : ISchemaFilter
{
    private static readonly HashSet<Type> StringEnums =
    [
        typeof(SecurityActivityType),
        typeof(AutomationExecutionStatus),
        typeof(Notrelix.Application.Features.Identity.Security.DTOs.StepUpRequiredFactor),
    ];

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!StringEnums.Contains(context.Type))
        {
            return;
        }

        schema.Type = "string";
        schema.Format = null;
        schema.Enum = Enum.GetNames(context.Type)
            .Select(name => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(name))
            .ToList();
    }
}
