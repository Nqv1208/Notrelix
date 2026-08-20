using Notrelix.API.Extensions;
using Notrelix.Application.Features.Identity.ApiTokens.DTOs;
using Notrelix.Application.Features.Identity.ApiTokens.Queries.ListApiTokens;

namespace Notrelix.API.Endpoints.Identity.ApiTokens.Queries;

public static class ListApiTokensEndpoint
{
    public static IEndpointRouteBuilder MapListApiTokens(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Identity.ApiTokens.List")
            .WithSummary("List API token metadata for a workspace")
            .WithDescription("Returns token metadata only. The raw secret is never returned by any read operation.")
            .Produces<IReadOnlyList<ApiTokenSummaryDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new ListApiTokensQuery(workspaceId));
        return result.ToApiResult();
    }
}