using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Mfa.Commands.DisableMfa;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class DisableMfaEndpoint
{
    public static IEndpointRouteBuilder MapDisableMfa(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/mfa/disable", HandleAsync)
            .WithName("Identity.Mfa.Disable")
            .WithTags("Identity.Mfa")
            .WithSummary("Disable MFA and revoke all active sessions");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new DisableMfaCommand());
        return result.ToApiResult();
    }
}