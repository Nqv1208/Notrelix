using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.BatchUpdateBlocks;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class BatchUpdateBlocksEndpoint
{
    public static IEndpointRouteBuilder MapBatchUpdateBlocks(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/batch", HandleAsync)
            .WithName("Documents.Blocks.BatchUpdateBlocks")
            .WithTags("Documents.Blocks")
            .WithSummary("Save several blocks as one change")
            .Produces<List<BlockDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, BatchUpdateBlocksRequest body, ISender sender)
    {
        var result = await sender.Send(new BatchUpdateBlocksCommand(
            pageId,
            body.Blocks.Select(block => new BatchUpdateBlockItem(block.Op, block.Id, block.Type, block.Content, block.Properties, block.Position, block.AfterBlockId, block.ParentId, block.ExpectedVersion)).ToList()
        ));
        return result.ToApiResult();
    }
}

