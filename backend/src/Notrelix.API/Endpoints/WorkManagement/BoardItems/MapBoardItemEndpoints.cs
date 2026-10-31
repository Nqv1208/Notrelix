using Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;
using Notrelix.API.Endpoints.WorkManagement.Approvals.Queries;
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
        boardItems.MapListBoardDependencies();
        boardItems.MapBulkUpdateBoardItems();
        boardItems.MapListBoardItemTrash();
        boardItems.MapListArchivedBoardItems();

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
        itemGroup.MapRemindBoardItemOwners();
        itemGroup.MapMoveBoardItem();
        itemGroup.MapMoveBoardItemToBoard();
        itemGroup.MapUpdateBoardItemFieldValues();
        itemGroup.MapLinkPageToBoardItem();
        itemGroup.MapUnlinkPageFromBoardItem();
        itemGroup.MapListBoardItemPageLinks();
        itemGroup.MapAssignBoardItemMember();
        itemGroup.MapUnassignBoardItemMember();
        itemGroup.MapAddLabelToBoardItem();
        itemGroup.MapRemoveLabelFromBoardItem();
        itemGroup.MapUpdateBoardItemFieldValue();
        itemGroup.MapClearFieldValue();
        itemGroup.MapRestoreBoardItem();
        itemGroup.MapCompleteBoardItem();
        itemGroup.MapReopenBoardItem();
        itemGroup.MapSetBoardItemDueDate();
        itemGroup.MapUpdateBoardItemStatus();
        itemGroup.MapListBoardItemLinks();
        itemGroup.MapCreateBoardItemLink();
        itemGroup.MapDeleteBoardItemLink();
        itemGroup.MapClearBoardItemLinks();
        itemGroup.MapSetBoardItemCover();
        itemGroup.MapSetBoardItemRepeat();
        itemGroup.MapSetBoardItemReminder();
        itemGroup.MapSetBoardItemVote();
        itemGroup.MapListBoardItemVotes();
        itemGroup.MapRecordBoardItemView();
        itemGroup.MapListBoardItemViewers();
        itemGroup.MapCreateChildItem();
        itemGroup.MapListChildItems();
        itemGroup.MapMoveChildItem();
        itemGroup.MapRequestItemApproval();
        itemGroup.MapListItemApprovalRequests();

        var myWork = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/my-work/items")
            .WithTags("WorkManagement.BoardItems")
            .WithOpenApi();

        myWork.MapListMyWorkItems();

        var myWorkExtended = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/my-work/extended")
            .WithTags("WorkManagement.BoardItems")
            .WithOpenApi();

        myWorkExtended.MapGetMyWorkExtended();

        var homeFeed = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/home/feed")
            .WithTags("WorkManagement.BoardItems")
            .WithOpenApi();

        homeFeed.MapGetHomeFeed();

        return app;
    }
}
