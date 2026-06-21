using Notrelix.API.Endpoints.Collaboration.Activity.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Activity;

public static class MapActivityEndpoints
{
    public static IEndpointRouteBuilder RegisterCardActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/cards/{cardId:guid}/activity")
            .WithTags("Collaboration.Activity")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGetCardActivity();

        return app;
    }
}
