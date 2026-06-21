using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Queries;

public static class GetCommentsEndpoint
{
    public static IEndpointRouteBuilder MapGetCardComments(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", GetCardCommentsAsync)
            .WithName("Collaboration.Comments.GetCardComments")
            .WithTags("Collaboration.Comments")
            .WithSummary("Get comments for a card");
        return group;
    }

    public static IEndpointRouteBuilder MapGetPageComments(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", GetPageCommentsAsync)
            .WithName("Collaboration.Comments.GetPageComments")
            .WithTags("Collaboration.Comments")
            .WithSummary("Get comments for a page");
        return group;
    }

    private static async Task<IResult> GetCardCommentsAsync(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new GetCommentsQuery("Card", cardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetPageCommentsAsync(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetCommentsQuery("Page", pageId));
        return result.ToApiResult();
    }
}
