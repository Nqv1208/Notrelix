using Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;
using Notrelix.API.Endpoints.WorkManagement.BoardItems.Queries;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems;

public static class MapBoardItemEndpoints
{
    public static IEndpointRouteBuilder RegisterBoardItemEndpoints(this IEndpointRouteBuilder app)
    {
        var boardItems = app
            .MapGroup("/api/v1/boards/{boardId:guid}/items")
            .WithTags("WorkManagement.BoardItems")
            .WithOpenApi();

        boardItems.MapCreateBoardItem();
        boardItems.MapListBoardItems();

        var itemGroup = app
            .MapGroup("/api/v1/board-items/{itemId:guid}")
            .WithTags("WorkManagement.BoardItems")
            .WithOpenApi();

        itemGroup.MapGetBoardItem();
        itemGroup.MapUpdateBoardItem();
        itemGroup.MapArchiveBoardItem();
        itemGroup.MapUnarchiveBoardItem();
        itemGroup.MapDeleteBoardItem();
        itemGroup.MapDuplicateBoardItem();
        itemGroup.MapMoveBoardItem();
        itemGroup.MapUpdateBoardItemFieldValues();
        itemGroup.MapLinkPageToBoardItem();
        itemGroup.MapUnlinkPageFromBoardItem();
        itemGroup.MapAssignBoardItemMember();
        itemGroup.MapUnassignBoardItemMember();
        itemGroup.MapAddLabelToBoardItem();
        itemGroup.MapRemoveLabelFromBoardItem();
        itemGroup.MapUpdateBoardItemFieldValue();
        itemGroup.MapClearFieldValue();
        itemGroup.MapRestoreBoardItem();
        itemGroup.MapCompleteBoardItem();
        itemGroup.MapSetBoardItemDueDate();
        itemGroup.MapUpdateBoardItemStatus();
        itemGroup.MapListBoardItemLinks();

        return app;
    }
}
