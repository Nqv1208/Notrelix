using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notrelix.API.OpenApi;

/// <summary>
/// Declares the extensions every problem response carries (<c>errorCode</c> category, <c>ruleCode</c>
/// refusal, <c>traceId</c>) so the generated client types them instead of reading an open bag.
/// </summary>
public sealed class ProblemDetailsSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(ProblemDetails))
        {
            return;
        }

        foreach (var name in new[] { "errorCode", "ruleCode", "traceId" })
        {
            schema.Properties[name] = new OpenApiSchema { Type = "string", Nullable = true };
        }
    }
}
