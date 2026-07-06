using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardViews.Queries.GetBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Queries;

public static class GetBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.BoardViews.Get")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Get current user's board view preference");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardViewQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
