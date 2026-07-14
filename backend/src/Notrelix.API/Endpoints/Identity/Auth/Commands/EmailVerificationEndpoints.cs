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
            .WithSummary("Confirm an email address with a one-time token");

        group.MapPublicPost("/email-verification/resend", ResendAsync)
            .WithName("Identity.Auth.ResendEmailVerification")
            .WithSummary("Request a new email verification message")
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));

        group.MapAuthenticatedPost("/email-verification/request", RequestAsync)
            .WithName("Identity.Auth.RequestEmailVerification")
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
