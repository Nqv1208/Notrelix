using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Shared.Comments;

namespace Notrelix.API.Endpoints.Comments;

public static class CommentEndpoints
{
    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        // Card comments
        var cardGroup = app
            .MapGroup("/api/cards/{cardId:guid}/comments")
            .WithTags("Comments")
            .RequireAuthorization()
            .WithOpenApi();

        cardGroup.MapGet("/", GetCardComments).WithName("GetCardComments");
        cardGroup.MapPost("/", CreateCardComment).WithName("CreateCardComment");

        // Page comments
        var pageGroup = app
            .MapGroup("/api/pages/{pageId:guid}/comments")
            .WithTags("Comments")
            .RequireAuthorization()
            .WithOpenApi();

        pageGroup.MapGet("/", GetPageComments).WithName("GetPageComments");
        pageGroup.MapPost("/", CreatePageComment).WithName("CreatePageComment");

        // Comment-scoped
        var group = app
            .MapGroup("/api/comments")
            .WithTags("Comments")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPatch("/{commentId:guid}", UpdateComment).WithName("UpdateComment");
        group.MapDelete("/{commentId:guid}", DeleteComment).WithName("DeleteComment");
        group.MapPost("/{commentId:guid}/resolve", ResolveComment).WithName("ResolveComment");

        return app;
    }

    private static async Task<IResult> GetCardComments(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new GetCommentsQuery("Card", cardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateCardComment(Guid cardId, CreateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCommentCommand("Card", cardId, body.ContentMd, body.ParentCommentId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetPageComments(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetCommentsQuery("Page", pageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreatePageComment(Guid pageId, CreateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCommentCommand("Page", pageId, body.ContentMd, body.ParentCommentId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateComment(Guid commentId, UpdateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateCommentCommand(commentId, body.ContentMd));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteComment(Guid commentId, ISender sender)
    {
        var result = await sender.Send(new DeleteCommentCommand(commentId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ResolveComment(Guid commentId, ISender sender)
    {
        var result = await sender.Send(new ResolveCommentCommand(commentId));
        return result.ToNoContentResult();
    }
}

public record CreateCommentRequest(string ContentMd, Guid? ParentCommentId = null);
public record UpdateCommentRequest(string ContentMd);
