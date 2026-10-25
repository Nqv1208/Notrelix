using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetMyWorkspaces;

namespace Notrelix.API.Endpoints.Workspaces.Members.Queries;

public static class ListMyWorkspacesEndpoint
{
    public static IEndpointRouteBuilder MapListMyWorkspaces(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedGet("/workspaces", HandleAsync)
            .WithName("Workspaces.Members.ListMyWorkspaces")
            .WithTags("Workspaces.Members")
            .WithSummary("List the workspaces you belong to, with your role and the default")
            .WithDescription("Only the caller's own active memberships of active workspaces, ordered by name. Member count is null for a Guest and department count is null unless the caller is an Owner or Admin. isDefault marks the workspace that opens with the app; canLeave is false only for the last active Owner.")
            .Produces<List<AccountWorkspaceDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new GetMyWorkspacesQuery());
        return result.ToApiResult();
    }
}
