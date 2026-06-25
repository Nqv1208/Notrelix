using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetInvitationByToken;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class GetInvitationByTokenEndpoint
{
    public static IEndpointRouteBuilder MapGetInvitationByToken(this IEndpointRouteBuilder group)
    {
        group.MapGet("/by-token/{token}", HandleAsync)
            .AllowAnonymous()
            .WithName("Workspaces.Invitations.GetInvitationByToken")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Get workspace invitation details by token");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string token,
        ISender sender)
    {
        var result = await sender.Send(new GetInvitationByTokenQuery(token));
        return result.ToApiResult();
    }
}
