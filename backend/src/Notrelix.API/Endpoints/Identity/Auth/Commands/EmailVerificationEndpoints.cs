using Notrelix.API.Contracts.Identity;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.Verification.Commands.ConfirmEmail;
using Notrelix.Application.Features.Identity.Verification.Commands.RequestEmailVerification;
using Notrelix.Application.Features.Identity.Verification.Commands.ResendEmailVerification;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class EmailVerificationEndpoints
{
    public static IEndpointRouteBuilder MapEmailVerification(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/email-verification/confirm", ConfirmAsync)
            .WithName("Identity.Auth.ConfirmEmail")
            .Produces<ConfirmEmailResultDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest)
            .WithSummary("Confirm an email address with a one-time token");

        group.MapPublicPost("/email-verification/resend", ResendAsync)
            .WithName("Identity.Auth.ResendEmailVerification")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status429TooManyRequests)
            .WithSummary("Request a new email verification message")
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));

        group.MapAuthenticatedPost("/email-verification/request", RequestAsync)
            .WithName("Identity.Auth.RequestEmailVerification")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized)
            .WithSummary("Request an email verification message for the current user");

        return group;
    }

    private static async Task<IResult> ConfirmAsync(
        OneTimeTokenRequest request,
        ISender sender)
        => (await sender.Send(new ConfirmEmailCommand(request.Token))).ToApiResult();

    private static async Task<IResult> ResendAsync(
        ResendEmailVerificationRequest request,
        ISender sender)
        => (await sender.Send(new ResendEmailVerificationCommand(request.Email))).ToApiResult();

    private static async Task<IResult> RequestAsync(ISender sender)
        => (await sender.Send(new RequestEmailVerificationCommand())).ToApiResult();

    private sealed record ResendEmailVerificationRequest(string Email);
}
