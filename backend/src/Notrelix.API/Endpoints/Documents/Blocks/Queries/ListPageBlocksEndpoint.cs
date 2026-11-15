using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;

namespace Notrelix.API.Endpoints.Documents.Blocks.Queries;

public static class ListPageBlocksEndpoint
{
    public static IEndpointRouteBuilder MapListPageBlocks(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Documents.Blocks.ListPageBlocks")
            .Produces<CursorPage<BlockDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Documents.Blocks")
            .WithSummary("List the blocks of a page, 1 000 at a time")
            .WithDescription("Position order, at most 1 000 a window. Pass nextCursor back as cursor for the next window.");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, string? cursor, int? limit, ISender sender)
    {
        var result = await sender.Send(new GetPageBlocksQuery(pageId, cursor, limit));
        return result.ToApiResult();
    }
}
