using Notrelix.API.Endpoints.WorkManagement.Templates.Commands;
using Notrelix.API.Endpoints.WorkManagement.Templates.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.Templates;

public static class MapTemplateEndpoints
{
    public static IEndpointRouteBuilder MapTemplates(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/templates")
            .WithTags("WorkManagement.Templates")
            .WithOpenApi();

        boardGroup.MapCreateBoardTemplate();

        var group = app
            .MapGroup("/api/v1/templates/{templateId:guid}")
            .WithTags("WorkManagement.Templates")
            .WithOpenApi();

        group.MapPublishBoardTemplate();
        group.MapArchiveBoardTemplate();
        group.MapDeleteBoardTemplate();
        group.MapCreateBoardFromTemplate();

        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/templates")
            .WithTags("WorkManagement.Templates")
            .WithOpenApi();

        workspaceGroup.MapListBoardTemplates();

        return app;
    }
}
