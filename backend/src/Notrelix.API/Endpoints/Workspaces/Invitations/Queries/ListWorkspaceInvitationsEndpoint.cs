using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class ListWorkspaceInvitationsEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspaceInvitations(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Workspaces.Invitations.ListWorkspaceInvitations")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Get workspace invitations")
            .WithDescription(
                "Cursor-paginated (default 50, max 200), newest invitation first. A pending invitation past its expiry " +
                "is reported as Expired. Pass nextCursor back as cursor for the next page.")
            .Produces<CursorPage<WorkspaceInvitationDto>>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceInvitationsQuery(workspaceId, cursor, limit));
        return result.ToApiResult();
    }
}
