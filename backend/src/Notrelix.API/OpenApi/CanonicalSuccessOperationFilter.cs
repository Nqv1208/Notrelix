using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notrelix.API.OpenApi;

/// <summary>
/// Removes the Swashbuckle default 200 response when an endpoint explicitly
/// declares a different canonical success code (201 Created, 204 No Content).
/// An endpoint that declares its real success contract must not also export a
/// phantom 200 from default IResult inference.
/// </summary>
public sealed class CanonicalSuccessOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses is null)
        {
            return;
        }

        var declared = context.ApiDescription.SupportedResponseTypes
            .Select(t => t.StatusCode)
            .Where(code => code >= 200 && code < 300 && code != 200)
            .ToList();

        if (declared.Count > 0)
        {
            operation.Responses.Remove("200");
        }
    }
}
