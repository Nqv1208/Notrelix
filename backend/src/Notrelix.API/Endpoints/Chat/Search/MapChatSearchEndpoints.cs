using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Search.Queries.SearchChatMessages;

namespace Notrelix.API.Endpoints.Chat.Search;

/// <summary>Search of the messages the caller may read now (UC-CHT-H07, CHT-022, CHT-004).</summary>
public static class MapChatSearchEndpoints
{
    public static IEndpointRouteBuilder MapChatSearchRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat")
            .WithTags("Chat.Search")
            .WithOpenApi();

        group.MapWorkspaceGet("/search", SearchAsync)
            .WithName("Chat.Search.Query")
            .WithSummary("Search messages in the spaces the caller can read")
            .WithDescription("q is required, 2 to 100 characters, matched as a phrase ignoring case and Vietnamese marks. Only messages of spaces the caller can read now are searched: public channels for a non-guest member, and a private channel, a direct or group conversation, or (for a Guest) any space only while the caller is an active member. A Workspace Owner or Admin gets nothing by role. Filters: spaceId, authorId, from (inclusive) and to (exclusive), hasAttachment, mentionsMe. Newest first by cursor; limit is 1 to 50, default 20. Each hit has a plain-text snippet with the match ranges (never HTML) and the jump target: spaceId, messageId and threadRootId for a thread reply. A space the caller cannot read gives an empty result, like a space with no hit; there is no total. Tombstones, system messages and messages past the retention window are never returned. Counted per person and per Workspace (chat.search); 429 over the limit. The text of q is never logged or audited.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static async Task<IResult> SearchAsync(
        Guid workspaceId,
        [FromQuery] string? q,
        [FromQuery] Guid? spaceId,
        [FromQuery] Guid? authorId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] bool? hasAttachment,
        [FromQuery] bool? mentionsMe,
        [FromQuery] string? cursor,
        [FromQuery] int? limit,
        ISender sender)
    {
        var result = await sender.Send(new SearchChatMessagesQuery(
            workspaceId, q ?? string.Empty, spaceId, authorId, from, to, hasAttachment, mentionsMe, cursor, limit ?? SearchChatMessagesQuery.DefaultLimit));
        return result.ToApiResult();
    }
}
