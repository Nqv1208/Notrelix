using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardDuplicatePreview;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardDuplicatePreviewEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardDuplicatePreview(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/duplicate-preview", HandleAsync)
            .WithName("WorkManagement.Boards.GetDuplicatePreview")
            .Produces<BoardDuplicatePreviewDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Count the items a copy would carry and the assignees it would drop");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        Guid? spaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardDuplicatePreviewQuery(boardId, spaceId), cancellationToken);
        return result.ToApiResult();
    }
}
