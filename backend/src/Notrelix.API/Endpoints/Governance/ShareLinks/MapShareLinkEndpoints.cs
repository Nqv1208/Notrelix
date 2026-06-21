using Notrelix.API.Endpoints.Governance.ShareLinks.Commands;

namespace Notrelix.API.Endpoints.Governance.ShareLinks;

public static class MapShareLinkEndpoints
{
    public static IEndpointRouteBuilder MapShareLinksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/resources/{resourceType}/{resourceId:guid}/share-links")
            .RequireAuthorization()
            .WithTags("Governance.ShareLinks")
            .WithOpenApi();

        group.MapCreateShareLink();
        group.MapDisableShareLink();

        return app;
    }
}
