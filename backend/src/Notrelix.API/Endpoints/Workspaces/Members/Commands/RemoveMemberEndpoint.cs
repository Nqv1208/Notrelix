using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class RemoveMemberEndpoint
{
    public static IEndpointRouteBuilder MapRemoveMember(this IEndpointRouteBuilder group)
    {
        group.MapDelete("/{userId:guid}", HandleAsync)
            .WithName("Workspaces.Members.RemoveMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Remove a member from workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new RemoveMemberCommand(workspaceId, userId));
        return result.ToNoContentResult();
    }
}
