using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Application.Features.Chat.Mentions.Queries.ListMentionables;

namespace Notrelix.API.Endpoints.Chat.Mentions;

/// <summary>The mention picker of a space (UC-CHT-G01, G02, G03). Mentions themselves travel with a message.</summary>
public static class MapMentionEndpoints
{
    public static IEndpointRouteBuilder MapMentionRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Mentions")
            .WithOpenApi();

        group.MapWorkspaceGet("/spaces/{spaceId:guid}/mentionables", ListMentionablesAsync)
            .WithName("Chat.Mentions.ListMentionables")
            .WithSummary("List the people a message in a space can mention")
            .WithDescription("The active members of the space (inSpace true) and, unless the caller is a Guest, the other active colleagues of the Workspace (inSpace false: the composer warns that they will not be notified and gain nothing). People are ids. canBroadcast says whether the caller may use @channel and @here; broadcastConfirmAbove is the member count above which a broadcast needs confirmBroadcast. A space the caller is not a member of is 404.")
            .Produces<MentionablesDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListMentionablesAsync(Guid workspaceId, Guid spaceId, ISender sender)
    {
        var result = await sender.Send(new ListMentionablesQuery(workspaceId, spaceId));
        return result.ToApiResult();
    }
}
