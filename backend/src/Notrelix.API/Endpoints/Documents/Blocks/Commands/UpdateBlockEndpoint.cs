using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.UpdateBlock;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class UpdateBlockEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBlock(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/{blockId:guid}", HandleAsync)
            .WithName("Documents.Blocks.UpdateBlock")
            .WithTags("Documents.Blocks")
            .WithSummary("Update a block against the version read")
            .Produces<BlockDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, Guid blockId, UpdateBlockRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateBlockCommand(pageId, blockId, body.Type, body.Content, body.Properties, body.ExpectedVersion));
        return result.ToApiResult();
    }
}

