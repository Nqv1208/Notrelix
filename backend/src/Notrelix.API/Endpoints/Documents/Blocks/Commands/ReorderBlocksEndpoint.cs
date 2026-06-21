using MediatR;
using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.ReorderBlocks;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class ReorderBlocksEndpoint
{
    public static IEndpointRouteBuilder MapReorderBlocks(this IEndpointRouteBuilder group)
    {
        group.MapPost("/reorder", HandleAsync)
            .WithName("Documents.Blocks.ReorderBlocks")
            .WithTags("Documents.Blocks")
            .WithSummary("Reorder blocks");
        return group;
    }

    private static async Task<IResult> HandleAsync(ReorderBlocksRequest body, ISender sender)
    {
        var result = await sender.Send(new ReorderBlocksCommand(
            body.PageId,
            body.Items.Select(item => new ReorderBlockItem(item.BlockId, item.Position.ToString(), item.ParentId)).ToList()
        ));
        return result.ToApiResult();
    }
}

