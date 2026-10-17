using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RemoveSpaceMember;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class RemoveSpaceMemberEndpoint
{
    public static IEndpointRouteBuilder MapRemoveSpaceMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/members/{subjectType}/{subjectId:guid}", HandleAsync)
            .WithName("Workspaces.Spaces.RemoveSpaceMember")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Remove a user or team entry from a space");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        string subjectType,
        Guid subjectId,
        ISender sender,
        [FromQuery] long expectedVersion)
    {
        var result = await sender.Send(new RemoveSpaceMemberCommand(workspaceId, spaceId, subjectType, subjectId, expectedVersion));
        return result.ToNoContentResult();
    }
}
