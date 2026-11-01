using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetDeletedWorkspaces;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Queries;

public static class GetDeletedWorkspacesEndpoint
{
    public static IEndpointRouteBuilder MapGetDeletedWorkspaces(this IEndpointRouteBuilder group)
    {
        group.MapAccountGet("/deleted", HandleAsync)
            .WithName("Workspaces.Workspaces.GetDeletedWorkspaces")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("List the workspaces the caller owns that were deleted inside the 30-day restore window")
            .Produces<List<DeletedWorkspaceDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(ISender sender)
    {
        var result = await sender.Send(new GetDeletedWorkspacesQuery());
        return result.ToApiResult();
    }
}
