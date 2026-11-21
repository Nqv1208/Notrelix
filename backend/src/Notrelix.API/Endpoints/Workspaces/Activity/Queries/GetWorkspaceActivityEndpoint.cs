using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.WorkspaceHome.Queries.GetWorkspaceActivity;

namespace Notrelix.API.Endpoints.Workspaces.Activity.Queries;

public static class GetWorkspaceActivityEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceActivity(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Activity.GetWorkspaceActivity")
            .WithTags("Workspaces.Activity")
            .WithSummary("Get workspace activity log")
            .WithDescription(
                "Cursor-paginated (default 20, max 50), newest first. A Guest is forbidden. Facts about a Board or Page " +
                "the caller cannot read are left out. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<WorkspaceActivityEntryDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceActivityQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
