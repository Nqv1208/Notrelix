using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.Queries.GetSecurityActivity;
using Notrelix.Application.Features.Identity.Security.Commands.CompleteStepUpMfa;
using Notrelix.Application.Features.Identity.Security.Commands.CompleteStepUpPassword;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Queries.GetAccountSecuritySummary;
using Notrelix.Application.Features.Identity.Security.Queries.GetStepUpRequirement;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class SecurityStepUpEndpoints
{
    public static IEndpointRouteBuilder MapSecurityStepUp(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/security/summary", GetSummaryAsync)
            .WithName("Identity.Security.GetSummary")
            .Produces<AccountSecuritySummaryDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Security")
            .WithSummary("Get the current user's sign-in security summary")
            .WithDescription("Password, MFA, active-session and linked-provider facts for the account security page. Contains no secret.")
            .WithMetadata(new RateLimitPolicyAttribute("GeneralAuthenticated"));

        group.MapAuthenticatedGet("/security/activity", GetActivityAsync)
            .WithName("Identity.Security.ListActivity")
            .Produces<CursorPage<SecurityActivityDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Security")
            .WithSummary("List the current user's security activity")
            .WithDescription(
                "Sign-in successes and failures, password changes and resets, MFA enabled or disabled and session revocations, " +
                "newest first, for the authenticated user only and kept for 90 days. Cursor-paginated (default 20, max 100): " +
                "pass nextCursor back as cursor for the next page. Contains no secret.")
            .WithMetadata(new RateLimitPolicyAttribute("GeneralAuthenticated"));

        group.MapAuthenticatedPost("/security/step-up/requirement", GetRequirementAsync)
            .WithName("Identity.Security.StepUpRequirement")
            .Produces<StepUpRequirementResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Security")
            .WithSummary("Get the step-up factor required for a security operation")
            .WithDescription("Returns the factor (MFA challenge, password or OAuth re-authentication) the current user must satisfy before a security-sensitive operation.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        group.MapAuthenticatedPost("/security/step-up/complete-mfa", CompleteMfaAsync)
            .WithName("Identity.Security.StepUpCompleteMfa")
            .Produces<StepUpProofResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Security")
            .WithSummary("Complete step-up verification with a TOTP or recovery code")
            .WithDescription("Verifies the MFA challenge bound to the current session and issues a single-use step-up proof.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        group.MapAuthenticatedPost("/security/step-up/complete-password", CompletePasswordAsync)
            .WithName("Identity.Security.StepUpCompletePassword")
            .Produces<StepUpProofResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Security")
            .WithSummary("Complete step-up verification with the current password")
            .WithDescription("Verifies the current password and issues a single-use step-up proof.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        return group;
    }

    private static async Task<IResult> GetSummaryAsync(ISender sender)
        => (await sender.Send(new GetAccountSecuritySummaryQuery())).ToApiResult();

    private static async Task<IResult> GetActivityAsync(string? cursor, int? limit, ISender sender)
        => (await sender.Send(new GetSecurityActivityQuery(cursor, limit))).ToApiResult();

    private static async Task<IResult> GetRequirementAsync(
        GetStepUpRequirementRequest request,
        ISender sender)
    {
        if (!StepUpPurposeInput.TryParse(request.Purpose, out var purpose))
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
        if (!StepUpPurposeInput.TryParse(request.Purpose, out var purpose))
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
        if (!StepUpPurposeInput.TryParse(request.Purpose, out var purpose))
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