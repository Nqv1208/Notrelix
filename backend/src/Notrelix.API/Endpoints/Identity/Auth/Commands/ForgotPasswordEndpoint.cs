using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Auth.Commands.ForgotPassword;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class ForgotPasswordEndpoint
{
    public static IEndpointRouteBuilder MapForgotPassword(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/forgot-password", HandleAsync)
            .WithName("Identity.Auth.ForgotPassword")
            .WithTags("Identity.Auth")
            .WithSummary("Request a password reset email")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        ForgotPasswordCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}
