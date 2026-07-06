using Notrelix.API.Endpoints.Documents.Pages.Commands;
using Notrelix.API.Endpoints.Documents.Pages.Queries;

namespace Notrelix.API.Endpoints.Documents.Pages;

public static class MapPageEndpoints
{
    public static IEndpointRouteBuilder AddPageEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/pages")
            .WithTags("Documents.Pages")
            .WithOpenApi();

        workspaceGroup.MapListWorkspacePages();
        workspaceGroup.MapGetPageTree();
        workspaceGroup.MapSearchPages();
        workspaceGroup.MapCreatePage();

        var pageGroup = app
            .MapGroup("/api/v1/pages/{pageId:guid}")
            .WithTags("Documents.Pages")
            .WithOpenApi();

        pageGroup.MapGetPage();
        pageGroup.MapUpdatePage();
        pageGroup.MapDeletePage();
        pageGroup.MapGetPageBreadcrumb();
        pageGroup.MapGetPageHistory();

        return app;
    }
}
