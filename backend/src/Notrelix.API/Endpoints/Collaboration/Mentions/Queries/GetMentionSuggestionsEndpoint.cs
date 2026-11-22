using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Mentions.DTOs;
using Notrelix.Application.Features.Collaboration.Mentions.Queries.GetMentionSuggestions;

namespace Notrelix.API.Endpoints.Collaboration.Mentions.Queries;

public static class GetMentionSuggestionsEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardItemMentionSuggestions(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", GetBoardItemMentionSuggestionsAsync)
            .WithName("Collaboration.Mentions.GetBoardItemMentionSuggestions")
            .Produces<List<MentionSuggestionDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Collaboration.Mentions")
            .WithSummary("Suggest people to mention on a board item")
            .WithDescription("Active, non-Guest workspace members who can see the item's board, never the caller. query matches the start of any word of the name (case and accents ignored, at most 64 characters); limit defaults to 8, at most 20.");
        return group;
    }

    public static IEndpointRouteBuilder MapGetPageMentionSuggestions(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", GetPageMentionSuggestionsAsync)
            .WithName("Collaboration.Mentions.GetPageMentionSuggestions")
            .Produces<List<MentionSuggestionDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Collaboration.Mentions")
            .WithSummary("Suggest people to mention on a page")
            .WithDescription("Active, non-Guest workspace members who can view the page, never the caller. query matches the start of any word of the name (case and accents ignored, at most 64 characters); limit defaults to 8, at most 20.");
        return group;
    }

    private static async Task<IResult> GetBoardItemMentionSuggestionsAsync(Guid boardItemId, string? query, int? limit, ISender sender)
    {
        var result = await sender.Send(GetMentionSuggestionsQuery.ForBoardItem(boardItemId, query, limit));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetPageMentionSuggestionsAsync(Guid pageId, string? query, int? limit, ISender sender)
    {
        var result = await sender.Send(GetMentionSuggestionsQuery.ForPage(pageId, query, limit));
        return result.ToApiResult();
    }
}
