using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class ChangePasswordEndpoint
{
    public static IEndpointRouteBuilder MapChangePassword(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/change-password", HandleAsync)
            .WithName("Identity.Auth.ChangePassword")
            .WithTags("Identity.Auth")
            .WithSummary("Change the authenticated user's password")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ChangePasswordRequest request,
        ISender sender)
    {
        var command = new ChangePasswordCommand
        {
            CurrentPassword = request.CurrentPassword,
            NewPassword = request.NewPassword
        };

        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}
