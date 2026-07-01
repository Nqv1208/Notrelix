using Notrelix.API.Endpoints.Admin;
using Notrelix.API.Endpoints.Automation.Executions;
using Notrelix.API.Endpoints.Automation.Rules;
using Notrelix.API.Endpoints.Collaboration.Activity;
using Notrelix.API.Endpoints.Collaboration.Attachments;
using Notrelix.API.Endpoints.Collaboration.Comments;
using Notrelix.API.Endpoints.Documents.Blocks;
using Notrelix.API.Endpoints.Documents.Pages;
using Notrelix.API.Endpoints.Governance.ResourcePermissions;
using Notrelix.API.Endpoints.Governance.ShareLinks;
using Notrelix.API.Endpoints.Health;
using Notrelix.API.Endpoints.Identity.Auth;
using Notrelix.API.Endpoints.Identity.Profile;
using Notrelix.API.Endpoints.WorkManagement.BoardFields;
using Notrelix.API.Endpoints.WorkManagement.BoardGroups;
using Notrelix.API.Endpoints.WorkManagement.BoardItems;
using Notrelix.API.Endpoints.WorkManagement.Boards;
using Notrelix.API.Endpoints.WorkManagement.BoardViews;
using Notrelix.API.Endpoints.WorkManagement.Checklists;
using Notrelix.API.Endpoints.WorkManagement.Labels;
using Notrelix.API.Endpoints.Workspaces.Activity;
using Notrelix.API.Endpoints.Workspaces.Invitations;
using Notrelix.API.Endpoints.Workspaces.Members;
using Notrelix.API.Endpoints.Workspaces.Workspaces;

namespace Notrelix.API.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        // Auth & Identity
        app.RegisterAuthEndpoints();
        app.RegisterProfileEndpoints();
        app.MapHealthEndpoints();

        // Workspace
        app.RegisterWorkspaceEndpoints();
        app.RegisterMemberEndpoints();
        app.RegisterInvitationEndpoints();
        app.RegisterWorkspaceActivityEndpoints();

        // Document
        app.AddPageEndpoints();
        app.AddBlockEndpoints();

        // WorkManagement - Boards
        app.RegisterWorkManagementBoardEndpoints();

        // WorkManagement - BoardFields
        app.RegisterBoardFieldEndpoints();

        // WorkManagement - BoardViews
        app.RegisterBoardViewEndpoints();

        // WorkManagement - BoardItems
        app.RegisterBoardItemEndpoints();

        // WorkManagement - BoardGroups
        app.MapBoardGroups();

        // WorkManagement - Checklists
        app.RegisterChecklistEndpoints();

        // WorkManagement - Labels
        app.MapLabels();

        // Collaboration
        app.RegisterCardActivityEndpoints();
        app.MapAttachmentsEndpoints();
        app.MapCommentsEndpoints();

        // Governance
        app.MapResourcePermissionsEndpoints();
        app.MapShareLinksEndpoints();

        // Automation
        app.MapRulesEndpoints();
        app.MapExecutionsEndpoints();

        // Admin
        app.MapOutboxDiagnosticsEndpoints();

        return app;
    }
}
