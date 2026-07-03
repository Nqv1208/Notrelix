using Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;
using Notrelix.API.Endpoints.WorkManagement.BoardViews.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews;

public static class MapBoardViewEndpoints
{
    public static IEndpointRouteBuilder RegisterBoardViewEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/boards/{boardId:guid}/views")
            .RequireAuthorization()
            .WithTags("WorkManagement.BoardViews")
            .WithOpenApi();

        group.MapGetBoardView();
        group.MapSaveBoardView();
        group.MapCreateBoardView();
        group.MapUpdateBoardViewConfig();
        group.MapDeleteBoardView();

        return app;
    }
}
