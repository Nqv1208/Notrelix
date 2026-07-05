using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.DeleteBlock;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class DeleteBlockEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBlock(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("Documents.Blocks.DeleteBlock")
            .WithTags("Documents.Blocks")
            .WithSummary("Delete a block");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid blockId, ISender sender)
    {
        var result = await sender.Send(new DeleteBlockCommand(blockId));
        return result.ToNoContentResult();
    }
}
