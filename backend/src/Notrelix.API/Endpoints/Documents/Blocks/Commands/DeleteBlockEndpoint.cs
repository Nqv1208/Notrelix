using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class DeleteBlockEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBlock(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{blockId:guid}", HandleAsync)
            .WithName("Documents.Blocks.DeleteBlock")
            .WithTags("Documents.Blocks")
            .WithSummary("Delete a block and the blocks under it against the version read")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, Guid blockId, [FromQuery] long expectedVersion, ISender sender)
    {
        var result = await sender.Send(new DeleteBlockCommand(pageId, blockId, expectedVersion));
        return result.ToNoContentResult();
    }
}
