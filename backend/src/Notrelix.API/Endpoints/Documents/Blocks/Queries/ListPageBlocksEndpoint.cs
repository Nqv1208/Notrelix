using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Queries.GetPageBlocks;

namespace Notrelix.API.Endpoints.Documents.Blocks.Queries;

public static class ListPageBlocksEndpoint
{
    public static IEndpointRouteBuilder MapListPageBlocks(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Documents.Blocks.ListPageBlocks")
            .WithTags("Documents.Blocks")
            .WithSummary("List blocks for a page");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageBlocksQuery(pageId));
        return result.ToApiResult();
    }
}
