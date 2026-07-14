using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class SuspendMemberEndpoint
{
    public static IEndpointRouteBuilder MapSuspendMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{userId:guid}/suspend", HandleAsync)
            .WithName("Workspaces.Members.SuspendMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Suspend a workspace member");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new SuspendMemberCommand(workspaceId, userId));
        return result.ToNoContentResult();
    }
}
