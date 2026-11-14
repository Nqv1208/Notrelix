using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
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
            .WithDescription("Returns token metadata only, newest first, as a cursor page: default 50, max 200 (limit). Pass nextCursor back as cursor for the next page. The raw secret is never returned by any read operation.")
            .Produces<CursorPage<ApiTokenSummaryDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new ListApiTokensQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}