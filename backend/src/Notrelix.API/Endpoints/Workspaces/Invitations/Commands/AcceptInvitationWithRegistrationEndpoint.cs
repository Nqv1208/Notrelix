using Notrelix.API.Contracts.Workspaces.Invitations.Requests;
using Notrelix.API.Contracts.Workspaces.Invitations.Responses;
using Notrelix.API.Extensions;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Workspaces.Invitations.Commands.AcceptInvitationWithRegistration;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Commands;

public static class AcceptInvitationWithRegistrationEndpoint
{
    public static IEndpointRouteBuilder MapAcceptInvitationWithRegistration(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/accept-with-registration", HandleAsync)
            .WithName("Workspaces.Invitations.AcceptInvitationWithRegistration")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Accept an invitation by creating the account in the same step")
            .WithDescription("Creates the sign-in identity for the invited address, joins the Workspace and signs the person in (httpOnly cookies). A repeat press answers the same success with alreadyAccepted and no session. An address that already has an account answers 409: sign in and accept instead.")
            .Produces<AcceptInvitationWithRegistrationResponse>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status429TooManyRequests)
            .WithMetadata(new RateLimitPolicyAttribute("AuthStrictByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        AcceptInvitationWithRegistrationRequest request,
        ISender sender,
        ICookieService cookieService)
    {
        var result = await sender.Send(
            new AcceptInvitationWithRegistrationCommand(request.Token, request.Name, request.Password));

        if (!result.Succeeded || result.Data is null)
            return result.ToApiResult();

        var accepted = result.Data;
        if (accepted.Session is { } session)
            cookieService.SetTokenCookie(session.AccessToken, session.RefreshToken);

        return Results.Ok(new AcceptInvitationWithRegistrationResponse
        {
            WorkspaceSlug = accepted.Acceptance.WorkspaceSlug,
            WorkspaceId = accepted.Acceptance.WorkspaceId,
            ResourceKind = accepted.Acceptance.ResourceKind,
            ResourceId = accepted.Acceptance.ResourceId,
            AlreadyAccepted = accepted.AlreadyAccepted,
            ExpiresAt = accepted.Session?.ExpiresAt,
            User = accepted.Session is { } user
                ? new UserDto { Id = user.UserId, Email = user.Email, Name = user.Name, EmailConfirmed = true }
                : null
        });
    }
}
