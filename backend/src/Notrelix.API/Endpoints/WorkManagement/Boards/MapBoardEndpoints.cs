using Notrelix.API.Endpoints.WorkManagement.Boards.Commands;
using Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.Boards;

public static class MapBoardEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkManagementBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var wsGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/boards")
            .WithTags("WorkManagement.Boards")
            .WithOpenApi();

        wsGroup.MapListWorkspaceBoards();
        wsGroup.MapCreateBoard();

        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}")
            .WithTags("WorkManagement.Boards")
            .WithOpenApi();

        boardGroup.MapGetBoard();
        boardGroup.MapRenameBoard();
        boardGroup.MapGetBoardOverview();
        boardGroup.MapArchiveBoard();
        boardGroup.MapUnarchiveBoard();
        boardGroup.MapDeleteBoard();
        boardGroup.MapRestoreBoard();
        boardGroup.MapUpdateBoardVisibility();

        var members = app
            .MapGroup("/api/v1/boards/{boardId:guid}/members")
            .WithTags("WorkManagement.Boards")
            .WithOpenApi();

        members.MapBoardMembers();

        return app;
    }
}
