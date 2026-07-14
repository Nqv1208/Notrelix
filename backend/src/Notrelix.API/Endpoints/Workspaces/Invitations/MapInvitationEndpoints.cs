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
            .WithOpenApi();

        wsGroup.MapListWorkspaceInvitations();
        wsGroup.MapCancelInvitation();
        wsGroup.MapResendInvitation();
        wsGroup.MapChangeInvitationRole();

        var invitationsGroup = app
            .MapGroup("/api/v1/invitations")
            .WithTags("Workspaces.Invitations")
            .WithOpenApi();

        invitationsGroup.MapGetUserPendingInvitations();
        invitationsGroup.MapAcceptInvitation();
        invitationsGroup.MapDeclineInvitation();
        invitationsGroup.MapGetInvitationByToken();

        return app;
    }
}
