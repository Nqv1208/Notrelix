using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.Mfa.Commands.RegenerateRecoveryCodes;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class RegenerateRecoveryCodesEndpoint
{
    public static IEndpointRouteBuilder MapRegenerateRecoveryCodes(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/mfa/recovery-codes/regenerate", HandleAsync)
            .WithName("Identity.Mfa.RegenerateRecoveryCodes")
            .WithTags("Identity.Mfa")
            .WithSummary("Invalidate existing recovery codes and issue a fresh batch");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new RegenerateRecoveryCodesCommand());
        return result.ToApiResult();
    }
}