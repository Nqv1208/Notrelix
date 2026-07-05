using Notrelix.API.Extensions;
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

    public static IEndpointRouteBuilder MapGetPageComments(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", GetPageCommentsAsync)
            .WithName("Collaboration.Comments.GetPageComments")
            .WithTags("Collaboration.Comments")
            .WithSummary("Get comments for a page");
        return group;
    }

    private static async Task<IResult> GetBoardItemCommentsAsync(Guid boardItemId, ISender sender)
    {
        var result = await sender.Send(new GetCommentsQuery(Enum.Parse<ResourceType>("BoardItem", ignoreCase: true), boardItemId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetPageCommentsAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetCommentsQuery(Enum.Parse<ResourceType>("Page", ignoreCase: true), pageId));
        return result.ToApiResult();
    }
}
