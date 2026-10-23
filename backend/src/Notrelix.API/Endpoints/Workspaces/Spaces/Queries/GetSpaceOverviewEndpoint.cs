using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceOverview;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Queries;

public static class GetSpaceOverviewEndpoint
{
    public static IEndpointRouteBuilder MapGetSpaceOverview(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/overview", HandleAsync)
            .WithName("Workspaces.Spaces.GetSpaceOverview")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Get the department overview as structured figures")
            .WithDescription("Head count by role with teams expanded, the running boards (not archived, readable by the caller, with at least one unfinished item) with progress and attention counts, recent root pages, attention counts, open work per person, child space counts and the access card. Finished boards count only in boardTotal. A space the caller cannot see is 404; as=member previews a plain member and is allowed only for a space manager (403 otherwise).")
            .Produces<SpaceOverviewDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        string? @as,
        ISender sender)
    {
        var result = await sender.Send(new GetSpaceOverviewQuery(workspaceId, spaceId, string.Equals(@as, "member", StringComparison.OrdinalIgnoreCase)));
        return result.ToApiResult();
    }
}
