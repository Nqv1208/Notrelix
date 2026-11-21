using Notrelix.API.Contracts.Identity;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;
using Notrelix.Application.Features.Identity.Mfa.Commands.RequiredEnrollment;
using Notrelix.Application.Features.Identity.Mfa.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

/// <summary>
/// The enrollment step of a sign-in the Account's MFA requirement stopped. The enrollment token from the
/// sign-in answer is the only credential here and no other operation accepts it.
/// </summary>
public static class RequiredMfaEnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapRequiredMfaEnrollment(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/mfa/required-enrollment/start", StartAsync)
            .WithName("Identity.Mfa.StartRequiredEnrollment")
            .Produces<MfaEnrollmentStartResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Start the authenticator the Account requires, before a session exists")
            .WithDescription("The enrollment token comes from the request body or, after an OAuth sign-in, from the httpOnly mfaChallenge cookie. Returns the secret and setup URI once.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));

        group.MapPublicPost("/mfa/required-enrollment/complete", CompleteAsync)
            .WithName("Identity.Mfa.CompleteRequiredEnrollment")
            .Produces<AuthSessionResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Mfa")
            .WithSummary("Activate the authenticator with its first code and finish the sign-in")
            .WithDescription("Issues the session and returns the one-time recovery codes. The token is consumed; the cookie is cleared on success and when the token is expired or exhausted.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> StartAsync(
        RequiredMfaEnrollmentStartRequest request,
        HttpContext httpContext,
        ISender sender,
        ICookieService cookieService)
    {
        // The answer holds a secret that is shown once: nothing may keep a copy.
        httpContext.Response.Headers.CacheControl = "no-store";
        var token = string.IsNullOrWhiteSpace(request.EnrollmentToken)
            ? cookieService.GetMfaChallengeCookie()
            : request.EnrollmentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<MfaEnrollmentStartResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid).ToApiResult();
        }

        var result = await sender.Send(new StartRequiredMfaEnrollmentCommand { EnrollmentToken = token });
        return result.ToApiResult();
    }

    private static async Task<IResult> CompleteAsync(
        RequiredMfaEnrollmentCompleteRequest request,
        HttpContext httpContext,
        ISender sender,
        ICookieService cookieService)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        var useCookie = string.IsNullOrWhiteSpace(request.EnrollmentToken);
        var token = useCookie ? cookieService.GetMfaChallengeCookie() : request.EnrollmentToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<AuthResult>.Failure(CompleteMfaChallengeErrors.ChallengeInvalid).ToSessionApiResult();
        }

        var result = await sender.Send(new CompleteRequiredMfaEnrollmentCommand
        {
            EnrollmentToken = token,
            MfaMethodId = request.MfaMethodId,
            Code = request.Code
        });

        if (result.Succeeded && result.Data is { AccessToken: not null } auth)
        {
            cookieService.SetTokenCookie(auth.AccessToken, auth.RefreshToken!);
            cookieService.DeleteMfaChallengeCookie();
        }
        else if (useCookie && result.TypedErrors.Any(e => e.Code == CompleteMfaChallengeErrors.ChallengeInvalidCode))
        {
            cookieService.DeleteMfaChallengeCookie();
        }

        return result.ToSessionApiResult();
    }
}

public sealed record RequiredMfaEnrollmentStartRequest
{
    /// <summary>Omitted after an OAuth sign-in: the token then travels in the httpOnly MFA challenge cookie.</summary>
    public string? EnrollmentToken { get; init; }
}

public sealed record RequiredMfaEnrollmentCompleteRequest
{
    public string? EnrollmentToken { get; init; }
    public Guid MfaMethodId { get; init; }
    public string Code { get; init; } = string.Empty;
}
