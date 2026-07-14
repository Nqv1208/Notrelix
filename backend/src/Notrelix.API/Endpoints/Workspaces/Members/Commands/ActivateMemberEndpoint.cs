using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.ActivateMember;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class ActivateMemberEndpoint
{
    public static IEndpointRouteBuilder MapActivateMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{userId:guid}/activate", HandleAsync)
            .WithName("Workspaces.Members.ActivateMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Activate a suspended workspace member");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new ActivateMemberCommand(workspaceId, userId));
        return result.ToNoContentResult();
    }
}
