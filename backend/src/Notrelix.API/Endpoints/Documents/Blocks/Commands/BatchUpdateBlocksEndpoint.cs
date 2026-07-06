using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class BatchUpdateBlocksEndpoint
{
    public static IEndpointRouteBuilder MapBatchUpdateBlocks(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/batch", HandleAsync)
            .WithName("Documents.Blocks.BatchUpdateBlocks")
            .WithTags("Documents.Blocks")
            .WithSummary("Batch update blocks");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, BatchUpdateBlocksRequest body, ISender sender)
    {
        var result = await sender.Send(new BatchUpdateBlocksCommand(
            pageId,
            body.Blocks.Select(block => new BatchUpdateBlockItem(block.Id, block.Type, block.Properties, block.Position?.ToString(), block.ParentId)).ToList()
        ));
        return result.ToApiResult();
    }
}

