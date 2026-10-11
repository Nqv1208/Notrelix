using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.AddSpaceMember;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class AddSpaceMemberEndpoint
{
    public static IEndpointRouteBuilder MapAddSpaceMember(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/members", HandleAsync)
            .WithName("Workspaces.Spaces.AddSpaceMember")
            .WithTags("Workspaces.Spaces")
            .WithSummary("Add a user or a team to a space with a space role");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid spaceId,
        AddSpaceMemberRequest request,
        ISender sender)
    {
        var result = await sender.Send(new AddSpaceMemberCommand(
            workspaceId, spaceId, request.SubjectType, request.SubjectId, request.Role, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
