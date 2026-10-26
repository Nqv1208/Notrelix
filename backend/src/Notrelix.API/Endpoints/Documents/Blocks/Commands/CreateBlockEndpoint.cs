using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;
using Notrelix.Application.Features.Documents.DTOs;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class CreateBlockEndpoint
{
    public static IEndpointRouteBuilder MapCreateBlock(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("Documents.Blocks.CreateBlock")
            .WithTags("Documents.Blocks")
            .WithSummary("Create a new block")
            .Produces<BlockDto>(StatusCodes.Status201Created, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, CreateBlockRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateBlockCommand(pageId, body.Id, body.Type, body.Content, body.Properties, body.AfterBlockId, body.ParentId));
        return result.ToCreatedResult(result.Data is { } block ? $"/api/v1/pages/{pageId}/blocks/{block.Id}" : null);
    }
}

