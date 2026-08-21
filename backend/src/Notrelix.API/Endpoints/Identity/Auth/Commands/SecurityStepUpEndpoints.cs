using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Security.Commands.CompleteStepUpMfa;
using Notrelix.Application.Features.Identity.Security.Commands.CompleteStepUpPassword;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Queries.GetStepUpRequirement;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class SecurityStepUpEndpoints
{
    public static IEndpointRouteBuilder MapSecurityStepUp(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/security/step-up/requirement", GetRequirementAsync)
            .WithName("Identity.Security.StepUpRequirement")
            .WithTags("Identity.Security")
            .WithSummary("Get the step-up factor required for a security operation")
            .WithDescription("Returns the factor (MFA challenge, password or OAuth re-authentication) the current user must satisfy before a security-sensitive operation.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        group.MapAuthenticatedPost("/security/step-up/complete-mfa", CompleteMfaAsync)
            .WithName("Identity.Security.StepUpCompleteMfa")
            .WithTags("Identity.Security")
            .WithSummary("Complete step-up verification with a TOTP or recovery code")
            .WithDescription("Verifies the MFA challenge bound to the current session and issues a single-use step-up proof.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        group.MapAuthenticatedPost("/security/step-up/complete-password", CompletePasswordAsync)
            .WithName("Identity.Security.StepUpCompletePassword")
            .WithTags("Identity.Security")
            .WithSummary("Complete step-up verification with the current password")
            .WithDescription("Verifies the current password and issues a single-use step-up proof.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        return group;
    }

    private static async Task<IResult> GetRequirementAsync(
        GetStepUpRequirementRequest request,
        ISender sender)
    {
        if (!Enum.TryParse<StepUpPurpose>(request.Purpose, ignoreCase: true, out var purpose))
        {
            return EndpointExtensions.InvalidInput($"Invalid step-up purpose: {request.Purpose}");
        }

        var result = await sender.Send(new GetStepUpRequirementQuery { Purpose = purpose });
        return result.ToApiResult();
    }

    private static async Task<IResult> CompleteMfaAsync(
        CompleteStepUpMfaRequest request,
        ISender sender)
    {
        if (!Enum.TryParse<StepUpPurpose>(request.Purpose, ignoreCase: true, out var purpose))
        {
            return EndpointExtensions.InvalidInput($"Invalid step-up purpose: {request.Purpose}");
        }

        var result = await sender.Send(new CompleteStepUpMfaCommand
        {
            Purpose = purpose,
            ChallengeToken = request.ChallengeToken,
            Code = request.Code
        });

        return result.ToApiResult();
    }

    private static async Task<IResult> CompletePasswordAsync(
        CompleteStepUpPasswordRequest request,
        ISender sender)
    {
        if (!Enum.TryParse<StepUpPurpose>(request.Purpose, ignoreCase: true, out var purpose))
        {
            return EndpointExtensions.InvalidInput($"Invalid step-up purpose: {request.Purpose}");
        }

        var result = await sender.Send(new CompleteStepUpPasswordCommand
        {
            Purpose = purpose,
            Password = request.Password
        });

        return result.ToApiResult();
    }
}

public sealed record GetStepUpRequirementRequest
{
    public string Purpose { get; init; } = string.Empty;
}

public sealed record CompleteStepUpMfaRequest
{
    public string Purpose { get; init; } = string.Empty;
    public string ChallengeToken { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public sealed record CompleteStepUpPasswordRequest
{
    public string Purpose { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}