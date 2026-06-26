using Notrelix.API.Contracts.Documents.Blocks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Documents.Blocks.Commands.CreateBlock;
using Notrelix.Domain.Documents.Blocks;

namespace Notrelix.API.Endpoints.Documents.Blocks.Commands;

public static class CreateBlockEndpoint
{
    public static IEndpointRouteBuilder MapCreateBlock(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("Documents.Blocks.CreateBlock")
            .WithTags("Documents.Blocks")
            .WithSummary("Create a new block");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid pageId, CreateBlockRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateBlockCommand(pageId, Enum.Parse<BlockType>(body.Type, ignoreCase: true), body.Properties ?? "{}", body.Position?.ToString() ?? "a0", body.ParentId));
        return result.ToCreatedResult();
    }
}

