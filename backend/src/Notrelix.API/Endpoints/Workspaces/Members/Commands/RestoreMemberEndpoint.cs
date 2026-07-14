using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.RestoreMember;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class RestoreMemberEndpoint
{
    public static IEndpointRouteBuilder MapRestoreMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{userId:guid}/restore", HandleAsync)
            .WithName("Workspaces.Members.RestoreMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Restore a removed workspace member");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new RestoreMemberCommand(workspaceId, userId));
        return result.ToNoContentResult();
    }
}
