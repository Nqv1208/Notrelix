using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Auth.Commands.ResetPassword;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class ResetPasswordEndpoint
{
    public static IEndpointRouteBuilder MapResetPassword(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/reset-password", HandleAsync)
            .WithName("Identity.Auth.ResetPassword")
            .WithTags("Identity.Auth")
            .WithSummary("Reset password with token")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ResetPasswordCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}
