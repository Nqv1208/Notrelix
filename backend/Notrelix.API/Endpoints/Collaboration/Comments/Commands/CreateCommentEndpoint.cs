using MediatR;
using Notrelix.API.Contracts.Collaboration.Comments.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class CreateCommentEndpoint
{
    public static IEndpointRouteBuilder MapCreateCardComment(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", CreateCardCommentAsync)
            .WithName("Collaboration.Comments.CreateCardComment")
            .WithTags("Collaboration.Comments")
            .WithSummary("Create a comment on a card");
        return group;
    }

    public static IEndpointRouteBuilder MapCreatePageComment(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", CreatePageCommentAsync)
            .WithName("Collaboration.Comments.CreatePageComment")
            .WithTags("Collaboration.Comments")
            .WithSummary("Create a comment on a page");
        return group;
    }

    private static async Task<IResult> CreateCardCommentAsync(Guid cardId, CreateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCommentCommand("Card", cardId, body.ContentMd, body.ParentCommentId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> CreatePageCommentAsync(Guid pageId, CreateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCommentCommand("Page", pageId, body.ContentMd, body.ParentCommentId));
        return result.ToCreatedResult();
    }
}

