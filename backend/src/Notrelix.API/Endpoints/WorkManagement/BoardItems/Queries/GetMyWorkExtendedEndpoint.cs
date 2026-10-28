using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyWorkExtended;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

public static class GetMyWorkExtendedEndpoint
{
    public static IEndpointRouteBuilder MapGetMyWorkExtended(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.GetMyWorkExtended")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Get the current user's My work in a workspace")
            .WithDescription(
                "Open Items assigned to the caller (soonest due first, at most 200) and the 50 most recently completed, " +
                "each with key, Status (label and category), Priority option and person in charge. Followed " +
                "Items and the open Items the caller is asked to approve are listed too. Waiting-on lists the open Items " +
                "the caller created or follows that only others are assigned to, soonest due first, with the person in " +
                "charge and when the caller last reminded them.")
            .Produces<MyWorkExtendedDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyWorkExtendedQuery(workspaceId), cancellationToken);
        return result.ToApiResult();
    }
}
