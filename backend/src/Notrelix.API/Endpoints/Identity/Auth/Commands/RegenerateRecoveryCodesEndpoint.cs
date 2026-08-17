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
            .WithSummary("Invalidate existing recovery codes and issue a fresh batch")
            .WithDescription("Requires a single-use step-up proof for the RegenerateRecoveryCodes purpose.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegenerateRecoveryCodesRequest request,
        ISender sender)
    {
        var result = await sender.Send(new RegenerateRecoveryCodesCommand { StepUpToken = request.StepUpToken });
        return result.ToApiResult();
    }
}

public sealed record RegenerateRecoveryCodesRequest
{
    public string StepUpToken { get; init; } = string.Empty;
}