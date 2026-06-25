using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;

namespace Notrelix.API.Endpoints.Workspaces.Members.Queries;

public static class ListMembersEndpoint
{
    public static IEndpointRouteBuilder MapListMembers(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Workspaces.Members.ListMembers")
            .WithTags("Workspaces.Members")
            .WithSummary("Get workspace members");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceMembersQuery(workspaceId));
        return result.ToApiResult();
    }
}
