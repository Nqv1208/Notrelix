using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Queries.GetWorkspaceMembers;

namespace Notrelix.API.Endpoints.Workspaces.Members.Queries;

public static class ListMembersEndpoint
{
    public static IEndpointRouteBuilder MapListMembers(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Members.ListMembers")
            .WithTags("Workspaces.Members")
            .WithSummary("Get workspace members")
            .WithDescription(
                "Cursor-paginated (default 50, max 200), oldest member first. Removed members are never listed. " +
                "Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<WorkspaceMemberDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceMembersQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
