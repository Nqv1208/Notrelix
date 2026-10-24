using Notrelix.API.Endpoints.Workspaces.Members.Commands;
using Notrelix.API.Endpoints.Workspaces.Members.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Members;

public static class MapMemberEndpoints
{
    public static IEndpointRouteBuilder RegisterMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/members")
            .WithTags("Workspaces.Members")
            .WithOpenApi();

        group.MapListMembers();
        group.MapGetMyMembership();
        group.MapInviteMember();
        group.MapAddMember();
        group.MapUpdateMemberRole();
        group.MapUpdateMemberJobTitle();
        group.MapRemoveMember();
        group.MapSuspendMember();
        group.MapActivateMember();

        // The signed-in person's own workspace memberships: what they belong to, the default, leaving.
        var account = app
            .MapGroup("/api/v1/account")
            .WithTags("Workspaces.Members")
            .WithOpenApi();

        account.MapListMyWorkspaces();
        account.MapSetDefaultWorkspace();
        account.MapLeaveWorkspace();
        return app;
    }
}
