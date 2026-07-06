using Notrelix.API.Contracts.Collaboration.Comments.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class CreateCommentEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardItemComment(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", CreateBoardItemCommentAsync)
            .WithName("Collaboration.Comments.CreateBoardItemComment")
            .WithTags("Collaboration.Comments")
            .WithSummary("Create a comment on a board item");
        return group;
    }

    public static IEndpointRouteBuilder MapCreatePageComment(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", CreatePageCommentAsync)
            .WithName("Collaboration.Comments.CreatePageComment")
            .WithTags("Collaboration.Comments")
            .WithSummary("Create a comment on a page");
        return group;
    }

    private static async Task<IResult> CreateBoardItemCommentAsync(Guid boardItemId, CreateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCommentCommand(Enum.Parse<ResourceType>("BoardItem", ignoreCase: true), boardItemId, body.ContentMd, body.ParentCommentId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> CreatePageCommentAsync(Guid pageId, CreateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateCommentCommand(Enum.Parse<ResourceType>("Page", ignoreCase: true), pageId, body.ContentMd, body.ParentCommentId));
        return result.ToCreatedResult();
    }
}

