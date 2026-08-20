using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.ApiTokens.Commands.RevokeApiToken;

namespace Notrelix.API.Endpoints.Identity.ApiTokens.Commands;

public static class RevokeApiTokenEndpoint
{
    public static IEndpointRouteBuilder MapRevokeApiToken(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/{tokenId:guid}", HandleAsync)
            .WithName("Identity.ApiTokens.Revoke")
            .WithSummary("Revoke an API token")
            .WithDescription("Revocation is effective immediately: a revoked token can no longer authenticate.")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid tokenId,
        ISender sender)
    {
        var result = await sender.Send(new RevokeApiTokenCommand(workspaceId, tokenId));
        return result.ToNoContentResult();
    }
}