using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Invitations.Queries.GetWorkspaceInvitations;

namespace Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

public static class ListWorkspaceInvitationsEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspaceInvitations(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Workspaces.Invitations.ListWorkspaceInvitations")
            .WithTags("Workspaces.Invitations")
            .WithSummary("Get workspace invitations");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceInvitationsQuery(workspaceId));
        return result.ToApiResult();
    }
}
