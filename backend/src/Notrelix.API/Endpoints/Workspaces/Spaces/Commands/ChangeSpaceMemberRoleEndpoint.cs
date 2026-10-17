using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.ChangeSpaceMemberRole;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class ChangeSpaceMemberRoleEndpoint
{
    public static IEndpointRouteBuilder MapChangeSpaceMemberRole(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePatch("/members/{subjectType}/{subjectId:guid}", HandleAsync)
            .WithName("Workspaces.Spaces.ChangeSpaceMemberRole")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Change the space role of a user or team entry");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        string subjectType,
        Guid subjectId,
        ChangeSpaceMemberRoleRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeSpaceMemberRoleCommand(
            workspaceId, spaceId, subjectType, subjectId, request.Role, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
