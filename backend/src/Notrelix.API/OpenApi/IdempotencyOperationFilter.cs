using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Notrelix.API.Idempotency;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notrelix.API.OpenApi;

/// <summary>
/// Declares the HTTP idempotency contract in OpenAPI for endpoints marked
/// with <see cref="IdempotencyEndpointExtensions.WithIdempotencyKey"/>:
/// a required Idempotency-Key header parameter, 409 payload-mismatch and
/// 503 incomplete-state responses, and the Idempotency-Replayed response
/// header on success responses. Unmarked operations are untouched.
/// </summary>
public sealed class IdempotencyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!HasIdempotencyMetadata(context.ApiDescription))
        {
            return;
        }

        if (operation.Parameters.All(p => p.Name != HttpIdempotencyEndpointFilter.KeyHeaderName))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = HttpIdempotencyEndpointFilter.KeyHeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description =
                    "Client-generated idempotency key (8-128 characters). The same key replayed " +
                    "with the same payload returns the stored response; reusing the key with a " +
                    "different payload returns 409.",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    MinLength = 8,
                    MaxLength = 128,
                },
            });
        }

        TryAddResponse(operation, "409", "Idempotency key reused with a different payload");
        TryAddResponse(operation, "503", "Operation still being processed; retry after the indicated delay");

        var successCodes = operation.Responses.Keys.Where(k => k.StartsWith('2')).ToList();
        if (successCodes.Count == 0)
        {
            // IResult handlers expose no inferred status; declare a success response
            // so the replay header has a documented home.
            operation.Responses["200"] = new OpenApiResponse { Description = "Success" };
            successCodes.Add("200");
        }

        foreach (var statusCode in successCodes)
        {
            var response = operation.Responses[statusCode];
            response.Headers ??= new Dictionary<string, OpenApiHeader>();
            if (!response.Headers.ContainsKey(HttpIdempotencyEndpointFilter.ReplayedHeaderName))
            {
                response.Headers[HttpIdempotencyEndpointFilter.ReplayedHeaderName] = new OpenApiHeader
                {
                    Description = "true when this response is a replay of a stored result",
                    Schema = new OpenApiSchema { Type = "boolean" },
                };
            }
        }
    }

    private static bool HasIdempotencyMetadata(ApiDescription apiDescription)
    {
        return apiDescription.ActionDescriptor.EndpointMetadata is not null
            && apiDescription.ActionDescriptor.EndpointMetadata
                .OfType<IdempotencyKeyRequiredMetadata>()
                .Any();
    }

    private static void TryAddResponse(OpenApiOperation operation, string statusCode, string description)
    {
        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses[statusCode] = new OpenApiResponse { Description = description };
        }
    }
}
