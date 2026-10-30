using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class ReorderBlocksEndpoint
{
    public static IEndpointRouteBuilder MapReorderBlocks(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/reorder", HandleAsync)
            .WithName("Documents.Blocks.ReorderBlocks")
            .WithTags("Documents.Blocks")
            .WithSummary("Move blocks of a page against the versions read")
            .Produces<List<BlockDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(ReorderBlocksRequest body, ISender sender)
    {
        var result = await sender.Send(new ReorderBlocksCommand(
            body.PageId,
            body.Items.Select(item => new ReorderBlockItem(item.BlockId, item.ExpectedVersion, item.ParentId, item.AfterBlockId, item.BeforeBlockId, item.Position)).ToList()
        ));
        return result.ToApiResult();
    }
}
