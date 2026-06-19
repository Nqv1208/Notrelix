using Notrelix.API.Endpoints.Identity.Auth.Commands;
using Notrelix.API.Endpoints.Identity.Auth.Queries;

namespace Notrelix.API.Endpoints.Identity.Auth;

public static class MapAuthEndpoints
{
    public static IEndpointRouteBuilder RegisterAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var publicGroup = app
            .MapGroup("/api/v1/auth")
            .WithTags("Identity.Auth")
            .WithOpenApi();

        publicGroup.MapRegister();
        publicGroup.MapLogin();
        publicGroup.MapForgotPassword();
        publicGroup.MapResetPassword();
        publicGroup.MapRefreshToken();

        var authGroup = app
            .MapGroup("/api/v1/auth")
            .WithTags("Identity.Auth")
            .RequireAuthorization()
            .WithOpenApi();

        authGroup.MapLogout();
        authGroup.MapGetCurrentUser();

        return app;
    }
}
