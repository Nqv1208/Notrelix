using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Collaboration.Comments.DTOs;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Queries;

public static class GetCommentsEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardItemComments(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", GetBoardItemCommentsAsync)
            .WithName("Collaboration.Comments.GetBoardItemComments")
            .WithTags("Collaboration.Comments")
            .WithSummary("Get comments for a board item");
        return group;
    }

    public static IEndpointRouteBuilder MapGetBoardItemCommentsPage(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/page", GetBoardItemCommentsPageAsync)
            .WithName("Collaboration.Comments.GetBoardItemCommentsPage")
            .Produces<CursorPage<CommentDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Collaboration.Comments")
            .WithSummary("Get a page of the comments of a board item")
            .WithDescription("Oldest first, default 50 and at most 200 a page. Pass nextCursor back as cursor for the next page.");
        return group;
    }

    public static IEndpointRouteBuilder MapGetPageComments(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", GetPageCommentsAsync)
            .WithName("Collaboration.Comments.GetPageComments")
            .Produces<List<CommentDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("Collaboration.Comments")
            .WithSummary("Get comments for a page");
        return group;
    }

    private static async Task<IResult> GetBoardItemCommentsAsync(Guid boardItemId, ISender sender)
    {
        var result = await sender.Send(GetCommentsQuery.ForBoardItem(boardItemId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetBoardItemCommentsPageAsync(Guid boardItemId, string? cursor, int? limit, ISender sender)
    {
        var take = TimeCursor.ClampLimit(limit);
        var result = await sender.Send(GetCommentsQuery.ForBoardItem(boardItemId) with { After = TimeCursor.Decode(cursor), Limit = take + 1 });
        if (!result.Succeeded) return result.ToApiResult();
        return Results.Ok(TimeCursor.Page(result.Data!, take, comment => (comment.CreatedAt, comment.Id)));
    }

    private static async Task<IResult> GetPageCommentsAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(GetCommentsQuery.ForPage(pageId));
        return result.ToApiResult();
    }
}
