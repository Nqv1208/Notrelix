using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardLifecycleImpact;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardLifecycleImpactEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardLifecycleImpact(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/lifecycle-impact", HandleAsync)
            .WithName("WorkManagement.Boards.GetLifecycleImpact")
            .Produces<BoardLifecycleImpactDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Count what archiving or deleting a board touches");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardLifecycleImpactQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
