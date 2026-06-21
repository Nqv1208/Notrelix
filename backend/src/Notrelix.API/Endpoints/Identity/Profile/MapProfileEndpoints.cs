using Notrelix.API.Endpoints.Identity.Profile.Commands;

namespace Notrelix.API.Endpoints.Identity.Profile;

public static class MapProfileEndpoints
{
    public static IEndpointRouteBuilder RegisterProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/profile")
            .WithTags("Identity.Profile")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapUpdateProfile();

        return app;
    }
}
