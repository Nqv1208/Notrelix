using Notrelix.API.Contracts.Workspaces.Members.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.UpdateMemberRole;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class UpdateMemberRoleEndpoint
{
    public static IEndpointRouteBuilder MapUpdateMemberRole(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/{userId:guid}", HandleAsync)
            .WithName("Workspaces.Members.UpdateMemberRole")
            .WithTags("Workspaces.Members")
            .WithSummary("Update a member's role");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid userId,
        UpdateMemberRoleRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateMemberRoleCommand(workspaceId, userId, Enum.Parse<WorkspaceRole>(body.Role, ignoreCase: true)));
        return result.ToApiResult();
    }
}
