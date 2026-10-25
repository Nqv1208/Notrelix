using Notrelix.API.Endpoints.Billing.Queries;

namespace Notrelix.API.Endpoints.Billing;

public static class BillingEndpointMappings
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/billing")
            .WithTags("Billing")
            .WithOpenApi();

        group.MapListWorkspaceBillingCapabilities();

        return app;
    }
}
