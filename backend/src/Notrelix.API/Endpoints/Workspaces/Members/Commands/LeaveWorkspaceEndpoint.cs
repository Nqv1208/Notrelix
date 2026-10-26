using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class LeaveWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapLeaveWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/workspaces/{workspaceId:guid}", HandleAsync)
            .WithName("Workspaces.Members.LeaveWorkspace")
            .WithTags("Workspaces.Members")
            .WithSummary("Leave a workspace")
            .WithDescription("Ends the caller's own membership. The last active Owner cannot leave and gets 409 workspaces.member.last-owner-cannot-leave. Revokes the caller's access to the workspace and clears it as the default.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new LeaveWorkspaceCommand(workspaceId));
        return result.ToNoContentResult();
    }
}
