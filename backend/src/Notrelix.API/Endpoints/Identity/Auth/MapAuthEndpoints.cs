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
        publicGroup.MapStartOAuthLogin();
        publicGroup.MapCompleteOAuthLogin();
        publicGroup.MapEmailVerification();

        var authGroup = app
            .MapGroup("/api/v1/auth")
            .WithTags("Identity.Auth")
            .WithOpenApi();

        authGroup.MapLogout();
        authGroup.MapChangePassword();
        authGroup.MapGetCurrentUser();
        authGroup.MapGetBootstrap();
        authGroup.MapStartOAuthLink();
        authGroup.MapCompleteOAuthLink();
        authGroup.MapUnlinkOAuth();

        return app;
    }
}
