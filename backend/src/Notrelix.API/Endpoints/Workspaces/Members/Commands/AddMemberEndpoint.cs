using Notrelix.API.Contracts.Workspaces.Members.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public static class AddMemberEndpoint
{
    public static IEndpointRouteBuilder MapAddMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/add", HandleAsync)
            .WithName("Workspaces.Members.AddMember")
            .WithTags("Workspaces.Members")
            .WithSummary("Add a member to the workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        AddMemberRequest request,
        ISender sender)
    {
        var result = await sender.Send(new AddMemberCommand(workspaceId, request.UserId, request.Role));
        return result.ToNoContentResult();
    }
}
