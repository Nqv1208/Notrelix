using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class UpdateBlockEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBlock(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithName("Documents.Blocks.UpdateBlock")
            .WithTags("Documents.Blocks")
            .WithSummary("Update a block");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid blockId, UpdateBlockRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateBlockCommand(blockId, body.Type, body.Properties));
        return result.ToApiResult();
    }
}

