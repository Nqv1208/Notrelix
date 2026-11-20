using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Identity.OAuth;
using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthStepUp;
using Notrelix.Application.Features.Identity.OAuth.DTOs;

namespace Notrelix.API.Endpoints.Identity.Auth.Commands;

public static class StartOAuthStepUpEndpoint
{
    public static IEndpointRouteBuilder MapStartOAuthStepUp(this IEndpointRouteBuilder group)
    {
        // A POST: a cross-site link or form cannot start it (the double-submit CSRF check refuses it), so a
        // page the person did not choose cannot send them to their provider for a purpose and target it picked.
        group.MapAuthenticatedPost("/oauth/{provider}/step-up/start", HandleAsync)
            .WithName("Identity.Auth.OAuth.StepUpStart")
            .Produces<OAuthLoginStartResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status412PreconditionFailed, StatusCodes.Status429TooManyRequests)
            .WithTags("Identity.Auth")
            .WithSummary("Start OAuth step-up (re-authentication) flow")
            .WithDescription(
                "Answers the provider URL the browser must go to, to re-authenticate for a security purpose; the provider is asked for a fresh sign-in (prompt=login, max_age=0). " +
                "returnUrl, when given, must be an internal root-relative path (anything else answers 400). " +
                "TransferWorkspaceOwnership also needs workspaceId and newOwnerId: the proof is bound to them and is refused for any other. " +
                "Completion grants a purpose-bound step-up proof; no new session is issued.")
            .WithMetadata(new RateLimitPolicyAttribute("AuthSensitiveByUser"));

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string provider,
        StartOAuthStepUpRequest request,
        ISender sender)
    {
        if (!Enum.TryParse<Domain.Identity.OAuth.OAuthProvider>(provider, ignoreCase: true, out var oauthProvider))
        {
            return EndpointExtensions.InvalidInput($"Invalid OAuth provider: {provider}");
        }

        if (!StepUpPurposeInput.TryParse(request.Purpose, out var stepUpPurpose))
        {
            return EndpointExtensions.InvalidInput($"Invalid step-up purpose: {request.Purpose}");
        }

        if (request.ReturnUrl is not null && !SafeReturnPath.IsSafe(request.ReturnUrl))
        {
            return EndpointExtensions.InvalidInput("returnUrl must be an internal path.");
        }

        if (!StepUpTargetInput.TryResolve(stepUpPurpose, request.WorkspaceId, request.NewOwnerId, request.MemberId, out var target))
        {
            return EndpointExtensions.InvalidInput(StepUpTargetInput.Missing);
        }

        var result = await sender.Send(new StartOAuthStepUpCommand
        {
            Provider = oauthProvider,
            Purpose = stepUpPurpose,
            ReturnUrl = request.ReturnUrl,
            Target = target
        });

        return result.ToApiResult();
    }
}

public sealed record StartOAuthStepUpRequest
{
    public string Purpose { get; init; } = string.Empty;
    public string? ReturnUrl { get; init; }

    /// <summary>The workspace of a handover proof (TransferWorkspaceOwnership only).</summary>
    public Guid? WorkspaceId { get; init; }

    /// <summary>The incoming Owner of a handover proof (TransferWorkspaceOwnership only).</summary>
    public Guid? NewOwnerId { get; init; }

    /// <summary>The member whose factors a reset proof is for (ResetMemberMfa only).</summary>
    public Guid? MemberId { get; init; }
}
