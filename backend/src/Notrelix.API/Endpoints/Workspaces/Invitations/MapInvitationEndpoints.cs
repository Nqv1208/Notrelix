using Notrelix.API.Endpoints.Workspaces.Invitations.Commands;
using Notrelix.API.Endpoints.Workspaces.Invitations.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Invitations;

public static class MapInvitationEndpoints
{
    public static IEndpointRouteBuilder RegisterInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var wsGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/invitations")
            .WithTags("Workspaces.Invitations")
            .RequireAuthorization()
            .WithOpenApi();

        wsGroup.MapListWorkspaceInvitations();
        wsGroup.MapCancelInvitation();

        var invitationsGroup = app
            .MapGroup("/api/v1/invitations")
            .WithTags("Workspaces.Invitations")
            .WithOpenApi();

        invitationsGroup.MapGetUserPendingInvitations();
        invitationsGroup.MapAcceptInvitation();

        invitationsGroup.MapGetInvitationByToken();

        return app;
    }
}
