using Notrelix.API.Endpoints.Collaboration.Mentions.Queries;

namespace Notrelix.API.Endpoints.Collaboration.Mentions;

public static class MapMentionSuggestionEndpoints
{
    public static IEndpointRouteBuilder MapMentionSuggestionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/board-items/{boardItemId:guid}/mention-suggestions")
            .WithTags("Collaboration.Mentions")
            .WithOpenApi()
            .MapGetBoardItemMentionSuggestions();

        app.MapGroup("/api/v1/pages/{pageId:guid}/mention-suggestions")
            .WithTags("Collaboration.Mentions")
            .WithOpenApi()
            .MapGetPageMentionSuggestions();

        return app;
    }
}
