using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Settings.Queries.GetWorkspaceSecurityExt;

namespace Notrelix.API.Endpoints.Workspaces.Settings.Queries;

public static class GetWorkspaceSecurityExtEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceSecurityExt(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Settings.GetWorkspaceSecurityExt")
            .WithTags("Workspaces.Settings")
            .WithSummary("Get the read-only extras of the sharing and security page")
            .WithDescription("Active Guest count, the default Guest expiry for new entries and the instant share links were turned off. Needs the right to see the member list, so a Guest is refused.")
            .Produces<WorkspaceSecurityExtDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceSecurityExtQuery(workspaceId));
        return result.ToApiResult();
    }
}
