using Notrelix.API.Endpoints.WorkManagement.Labels.Commands;
using Notrelix.API.Endpoints.WorkManagement.Labels.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.Labels;

public static class MapLabelEndpoints
{
    public static IEndpointRouteBuilder MapLabels(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/labels")
            .WithTags("WorkManagement.Labels")
            .WithOpenApi();

        boardGroup.MapCreateLabel();
        boardGroup.MapListLabels();

        var group = app
            .MapGroup("/api/v1/labels/{labelId:guid}")
            .WithTags("WorkManagement.Labels")
            .WithOpenApi();

        group.MapUpdateLabel();
        group.MapDeleteLabel();

        return app;
    }
}
