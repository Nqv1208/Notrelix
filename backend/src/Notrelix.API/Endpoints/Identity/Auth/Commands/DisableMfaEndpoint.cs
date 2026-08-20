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
            .WithSummary("Disable MFA and revoke all active sessions")
            .WithDescription("Requires a single-use step-up proof for the DisableMfa purpose.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        DisableMfaRequest request,
        ISender sender)
    {
        var result = await sender.Send(new DisableMfaCommand { StepUpToken = request.StepUpToken });
        return result.ToApiResult();
    }
}

public sealed record DisableMfaRequest
{
    public string StepUpToken { get; init; } = string.Empty;
}