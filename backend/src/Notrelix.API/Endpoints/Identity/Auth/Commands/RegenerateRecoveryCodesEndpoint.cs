using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Mfa.Commands.RegenerateRecoveryCodes;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class RegenerateRecoveryCodesEndpoint
{
    public static IEndpointRouteBuilder MapRegenerateRecoveryCodes(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/mfa/recovery-codes/regenerate", HandleAsync)
            .WithName("Identity.Mfa.RegenerateRecoveryCodes")
            .Produces<MfaEnrollmentVerifyResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Invalidate existing recovery codes and issue a fresh batch")
            .WithDescription("Requires a single-use step-up proof for the RegenerateRecoveryCodes purpose. The codes in the answer are shown once and never readable again; a retry makes another batch and voids this one.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegenerateRecoveryCodesRequest request,
        HttpContext httpContext,
        ISender sender)
    {
        // The answer holds recovery codes that are shown once: nothing may keep a copy.
        httpContext.Response.Headers.CacheControl = "no-store";
        var result = await sender.Send(new RegenerateRecoveryCodesCommand { StepUpToken = request.StepUpToken });
        return result.ToApiResult();
    }
}

public sealed record RegenerateRecoveryCodesRequest
{
    public string StepUpToken { get; init; } = string.Empty;
}