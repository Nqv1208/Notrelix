using Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups;

public static class MapBoardGroupEndpoints
{
    public static IEndpointRouteBuilder MapBoardGroups(this IEndpointRouteBuilder app)
    {
        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/groups")
            .RequireAuthorization()
            .WithTags("WorkManagement.BoardGroups")
            .WithOpenApi();

        boardGroup.MapCreateBoardGroup();
        boardGroup.MapReorderBoardGroups();

        var group = app
            .MapGroup("/api/v1/board-groups/{groupId:guid}")
            .RequireAuthorization()
            .WithTags("WorkManagement.BoardGroups")
            .WithOpenApi();

        group.MapUpdateBoardGroup();
        group.MapDuplicateBoardGroup();
        group.MapArchiveBoardGroup();
        group.MapUnarchiveBoardGroup();

        return app;
    }
}
