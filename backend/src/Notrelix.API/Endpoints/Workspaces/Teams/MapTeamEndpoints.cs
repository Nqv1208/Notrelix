using Notrelix.API.Endpoints.Workspaces.Teams.Commands;
using Notrelix.API.Endpoints.Workspaces.Teams.Queries;

namespace Notrelix.API.Endpoints.Workspaces.Teams;

public static class MapTeamEndpoints
{
    public static IEndpointRouteBuilder RegisterTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/teams")
            .WithTags("Workspaces.Teams")
            .WithOpenApi();

        group.MapGetWorkspaceTeams();
        group.MapCreateTeam();

        var byIdGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/teams/{teamId:guid}")
            .WithTags("Workspaces.Teams")
            .WithOpenApi();

        byIdGroup.MapGetTeam();
        byIdGroup.MapRenameTeam();
        byIdGroup.MapUpdateTeamDescription();
        byIdGroup.MapArchiveTeam();
        byIdGroup.MapUnarchiveTeam();
        byIdGroup.MapDeleteTeam();
        byIdGroup.MapRestoreTeam();

        var membersGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/teams/{teamId:guid}/members")
            .WithTags("Workspaces.Teams.Members")
            .WithOpenApi();

        membersGroup.MapAddTeamMember();
        membersGroup.MapRemoveTeamMember();
        membersGroup.MapChangeTeamMemberRole();

        return app;
    }
}
