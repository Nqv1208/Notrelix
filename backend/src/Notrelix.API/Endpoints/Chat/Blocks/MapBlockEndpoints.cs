using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Blocks.Commands.SetPersonBlock;

namespace Notrelix.API.Endpoints.Chat.Blocks;

/// <summary>The caller's blocks of other people in the Workspace (UC-CHT-E06).</summary>
public static class MapBlockEndpoints
{
    public static IEndpointRouteBuilder MapBlockRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/me/blocks")
            .WithTags("Chat.Blocks")
            .WithOpenApi();

        group.MapWorkspacePut("/", SetAsync)
            .WithName("Chat.Blocks.Set")
            .WithSummary("Block or unblock a person for the caller")
            .WithDescription("A block stops a direct conversation between the caller and that person from being started by either side (404). It does not hide channel messages. Idempotent. Blocking yourself is 400; a person who is not in the Workspace is 404.")
            .Produces<Application.Features.Chat.Blocks.DTOs.PersonBlockDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> SetAsync(Guid workspaceId, SetPersonBlockRequest body, ISender sender)
    {
        if (body.UserId is not { } userId)
            return EndpointExtensions.InvalidInput("userId is required.");
        if (body.Blocked is not { } blocked)
            return EndpointExtensions.InvalidInput("blocked is required.");

        var result = await sender.Send(new SetPersonBlockCommand(workspaceId, userId, blocked));
        return result.ToApiResult();
    }
}
