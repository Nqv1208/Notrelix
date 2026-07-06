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
        group.MapInviteMember();
        group.MapUpdateMemberRole();
        group.MapRemoveMember();

        return app;
    }
}
