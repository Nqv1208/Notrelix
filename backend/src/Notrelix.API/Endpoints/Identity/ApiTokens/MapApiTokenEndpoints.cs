using Notrelix.API.Endpoints.Identity.ApiTokens.Commands;
using Notrelix.API.Endpoints.Identity.ApiTokens.Queries;

namespace Notrelix.API.Endpoints.Identity.ApiTokens;

public static class MapApiTokenEndpoints
{
    public static IEndpointRouteBuilder RegisterApiTokenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/api-tokens")
            .WithTags("Identity.ApiTokens")
            .WithOpenApi();

        group.MapListApiTokens();
        group.MapCreateApiToken();
        group.MapRevokeApiToken();

        return app;
    }
}