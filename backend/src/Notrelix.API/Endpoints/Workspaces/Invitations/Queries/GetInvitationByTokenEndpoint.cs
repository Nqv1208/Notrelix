using Notrelix.API.Extensions;
using Notrelix.API.Contracts.Identity;
using Notrelix.API.RateLimiting;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class GetInvitationByTokenEndpoint
{
    public static IEndpointRouteBuilder MapGetInvitationByToken(this IEndpointRouteBuilder group)
    {
        group.MapPublicPost("/preview", HandleAsync)
            .WithName("Workspaces.Invitations.GetInvitationByToken")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Read an invitation link before signing in")
            .WithDescription("Answers valid, expired or used with the inviter's display name, the Workspace name and, for a guest invitation, the resource title and level. The invited email and whether it has an account are returned only while the invitation is valid.")
            .Produces<InvitationPreviewDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status429TooManyRequests)
            .WithMetadata(new RateLimitPolicyAttribute("AuthCredentialByIp"));
        return group;
    }

    private static async Task<IResult> HandleAsync(
        OneTimeTokenRequest request,
        ISender sender)
    {
        var result = await sender.Send(new GetInvitationByTokenQuery(request.Token));
        return result.ToApiResult();
    }
}
