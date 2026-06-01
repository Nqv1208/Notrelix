using MediatR;
using Notrelix.Application.Features.Boards.Commands.BoardColumns.CreateBoardColumn;
using Notrelix.Application.Features.Boards.Commands.BoardColumns.DeleteBoardColumn;
using Notrelix.Application.Features.Boards.Commands.BoardColumns.ReorderBoardColumns;
using Notrelix.Application.Features.Boards.Commands.BoardColumns.UpdateBoardColumn;
using Notrelix.Application.Features.Boards.Commands.BoardLists.ArchiveList;
using Notrelix.Application.Features.Boards.Commands.BoardLists.CreateList;
using Notrelix.Application.Features.Boards.Commands.BoardLists.DuplicateList;
using Notrelix.Application.Features.Boards.Commands.BoardLists.ReorderLists;
using Notrelix.Application.Features.Boards.Commands.BoardLists.UnarchiveList;
using Notrelix.Application.Features.Boards.Commands.BoardLists.UpdateList;
using Notrelix.Application.Features.Boards.Commands.Boards.AddBoardMember;
using Notrelix.Application.Features.Boards.Commands.Boards.ArchiveBoard;
using Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardBySlug;
using Notrelix.Application.Features.Boards.Commands.Boards.CreateBoardInWorkspace;
using Notrelix.Application.Features.Boards.Commands.Boards.RemoveBoardMember;
using Notrelix.Application.Features.Boards.Commands.Boards.SaveBoardView;
using Notrelix.Application.Features.Boards.Commands.Boards.UnarchiveBoard;
using Notrelix.Application.Features.Boards.Commands.Boards.UpdateBoard;
using Notrelix.Application.Features.Boards.Commands.CardLinks.CreateCardLink;
using Notrelix.Application.Features.Boards.Commands.CardLinks.DeleteCardLink;
using Notrelix.Application.Features.Boards.Commands.CardMembers.AssignCardMember;
using Notrelix.Application.Features.Boards.Commands.CardMembers.UnassignCardMember;
using Notrelix.Application.Features.Boards.Commands.Cards.ArchiveCard;
using Notrelix.Application.Features.Boards.Commands.Cards.CreateCard;
using Notrelix.Application.Features.Boards.Commands.Cards.DuplicateCard;
using Notrelix.Application.Features.Boards.Commands.Cards.LinkPageToCard;
using Notrelix.Application.Features.Boards.Commands.Cards.MoveCard;
using Notrelix.Application.Features.Boards.Commands.Cards.SetCardDueDate;
using Notrelix.Application.Features.Boards.Commands.Cards.UnlinkPageFromCard;
using Notrelix.Application.Features.Boards.Commands.Cards.UpdateCard;
using Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardFieldValues;
using Notrelix.Application.Features.Boards.Commands.Cards.UpdateCardStatus;
using Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklist;
using Notrelix.Application.Features.Boards.Commands.Checklists.CreateChecklistItem;
using Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklist;
using Notrelix.Application.Features.Boards.Commands.Checklists.DeleteChecklistItem;
using Notrelix.Application.Features.Boards.Commands.Checklists.ToggleChecklistItem;
using Notrelix.Application.Features.Boards.Commands.Checklists.UpdateChecklist;
using Notrelix.Application.Features.Boards.Commands.Checklists.UpdateChecklistItem;
using Notrelix.Application.Features.Boards.Commands.Common;
using Notrelix.Application.Features.Boards.Commands.Labels.AddLabelToCard;
using Notrelix.Application.Features.Boards.Commands.Labels.CreateLabel;
using Notrelix.Application.Features.Boards.Commands.Labels.DeleteLabel;
using Notrelix.Application.Features.Boards.Commands.Labels.RemoveLabelFromCard;
using Notrelix.Application.Features.Boards.Commands.Labels.UpdateLabel;
using Notrelix.Application.Features.Boards.DTOs;
using Notrelix.Application.Features.Boards.Queries.GetBoard;
using Notrelix.Application.Features.Boards.Queries.GetBoardMembers;
using Notrelix.Application.Features.Boards.Queries.GetBoardView;
using Notrelix.Application.Features.Boards.Queries.GetBoards;
using Notrelix.Application.Features.Boards.Queries.GetBoardsBySlug;
using Notrelix.Application.Features.Boards.Queries.GetCard;
using Notrelix.Application.Features.Boards.Queries.GetChecklists;
using Notrelix.Application.Features.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.Boards.Queries.GetLabels;
using Notrelix.Application.Features.Boards.Queries.GetMyCards;
using Notrelix.Application.Features.Calendar.Commands.ConnectCalendar;
using Notrelix.Application.Features.Calendar.Commands.DisconnectCalendar;
using Notrelix.Application.Features.Calendar.Commands.HandleCalendarWebhook;
using Notrelix.Application.Features.Calendar.Commands.TriggerCalendarSync;
using Notrelix.Application.Features.Document.Commands.Blocks.BatchUpdateBlocks;
using Notrelix.Application.Features.Document.Commands.Blocks.CreateBlock;
using Notrelix.Application.Features.Document.Commands.Blocks.DeleteBlock;
using Notrelix.Application.Features.Document.Commands.Blocks.ReorderBlocks;
using Notrelix.Application.Features.Document.Commands.Blocks.UpdateBlock;
using Notrelix.Application.Features.Document.Commands.Pages.ArchivePage;
using Notrelix.Application.Features.Document.Commands.Pages.CreatePage;
using Notrelix.Application.Features.Document.Commands.Pages.DeletePage;
using Notrelix.Application.Features.Document.Commands.Pages.MovePage;
using Notrelix.Application.Features.Document.Commands.Pages.PublishPage;
using Notrelix.Application.Features.Document.Commands.Pages.SetPageDeadline;
using Notrelix.Application.Features.Document.Commands.Pages.UpdatePage;
using Notrelix.Application.Features.Document.Common;
using Notrelix.Application.Features.Document.DTOs;
using Notrelix.Application.Features.Document.Queries.GetPage;
using Notrelix.Application.Features.Document.Queries.GetPageBlocks;
using Notrelix.Application.Features.Document.Queries.GetPageBreadcrumb;
using Notrelix.Application.Features.Document.Queries.GetPageHistory;
using Notrelix.Application.Features.Document.Queries.GetPageTree;
using Notrelix.Application.Features.Document.Queries.GetWorkspacePages;
using Notrelix.Application.Features.Document.Queries.SearchPages;
using Notrelix.Application.Features.Shared.Activity.DTOs;
using Notrelix.Application.Features.Shared.Attachments.DTOs;
using Notrelix.Application.Features.Shared.Commands.Attachments.CreateCardAttachment;
using Notrelix.Application.Features.Shared.Commands.Comments.CreateComment;
using Notrelix.Application.Features.Shared.Commands.Comments.DeleteComment;
using Notrelix.Application.Features.Shared.Commands.Comments.ResolveComment;
using Notrelix.Application.Features.Shared.Commands.Comments.UpdateComment;
using Notrelix.Application.Features.Shared.Comments.DTOs;
using Notrelix.Application.Features.Shared.Queries.Activity.GetResourceActivity;
using Notrelix.Application.Features.Shared.Queries.Attachments.GetCardAttachments;
using Notrelix.Application.Features.Shared.Queries.Comments.GetComments;
using Notrelix.Application.Features.Workspaces.Commands.AcceptInvitation;
using Notrelix.Application.Features.Workspaces.Commands.ArchiveWorkspace;
using Notrelix.Application.Features.Workspaces.Commands.ArchiveWorkspaceBySlug;
using Notrelix.Application.Features.Workspaces.Commands.CreateWorkspace;
using Notrelix.Application.Features.Workspaces.Commands.InviteMember;
using Notrelix.Application.Features.Workspaces.Commands.InviteMemberBySlug;
using Notrelix.Application.Features.Workspaces.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Commands.RemoveMemberBySlug;
using Notrelix.Application.Features.Workspaces.Commands.UpdateMemberRole;
using Notrelix.Application.Features.Workspaces.Commands.UpdateMemberRoleBySlug;
using Notrelix.Application.Features.Workspaces.Commands.UpdateWorkspace;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Queries.GetUserWorkspaces;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspace;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceActivity;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceActivityBySlug;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceBySlug;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceInvitations;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceMembers;
using Notrelix.Application.Features.Workspaces.Queries.GetWorkspaceMembersBySlug;
using Notrelix.API.Extensions;
namespace Notrelix.API.Endpoints.Document;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var workspaceGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/pages")
            .WithTags("Document")
            .RequireAuthorization()
            .WithOpenApi();

        workspaceGroup.MapGet("/", GetWorkspacePages).WithName("GetWorkspacePages");
        workspaceGroup.MapGet("/tree", GetPageTree).WithName("GetPageTree");
        workspaceGroup.MapGet("/search", SearchPages).WithName("SearchPages");
        workspaceGroup.MapPost("/", CreatePage).WithName("CreatePage");

        var pageGroup = app
            .MapGroup("/api/v1/pages")
            .WithTags("Document")
            .RequireAuthorization()
            .WithOpenApi();

        pageGroup.MapGet("/{pageId:guid}", GetPage).WithName("GetPage");
        pageGroup.MapPatch("/{pageId:guid}", UpdatePage).WithName("UpdatePage");
        pageGroup.MapDelete("/{pageId:guid}", DeletePage).WithName("DeletePage");
        pageGroup.MapGet("/{pageId:guid}/breadcrumb", GetBreadcrumb).WithName("GetPageBreadcrumb");
        pageGroup.MapGet("/{pageId:guid}/history", GetHistory).WithName("GetPageHistory");
        pageGroup.MapGet("/{pageId:guid}/blocks", GetBlocks).WithName("GetPageBlocks");
        pageGroup.MapPost("/{pageId:guid}/blocks", CreateBlock).WithName("CreateBlock");
        pageGroup.MapPost("/{pageId:guid}/blocks/batch", BatchUpdateBlocks).WithName("BatchUpdateBlocks");

        var blockGroup = app
            .MapGroup("/api/v1/blocks")
            .WithTags("Document")
            .RequireAuthorization()
            .WithOpenApi();

        blockGroup.MapPatch("/{blockId:guid}", UpdateBlock).WithName("UpdateBlock");
        blockGroup.MapDelete("/{blockId:guid}", DeleteBlock).WithName("DeleteBlock");
        blockGroup.MapPost("/reorder", ReorderBlocks).WithName("ReorderBlocks");

        return app;
    }

    private static async Task<IResult> GetWorkspacePages(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetWorkspacePagesQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetPageTree(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetPageTreeQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> SearchPages(Guid workspaceId, string query, ISender sender)
    {
        var result = await sender.Send(new SearchPagesQuery(workspaceId, query));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreatePage(Guid workspaceId, CreatePageRequest body, ISender sender)
    {
        var result = await sender.Send(new CreatePageCommand(workspaceId, body.Title, body.ParentId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetPage(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageQuery(pageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdatePage(Guid pageId, UpdatePageRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdatePageCommand(pageId, body.Title, body.IconType, body.IconValue, body.CoverUrl));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeletePage(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new DeletePageCommand(pageId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> GetBreadcrumb(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageBreadcrumbQuery(pageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetHistory(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageHistoryQuery(pageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetBlocks(Guid pageId, ISender sender)
    {
        var result = await sender.Send(new GetPageBlocksQuery(pageId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateBlock(Guid pageId, CreateBlockRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateBlockCommand(pageId, body.Type, body.Properties ?? "{}", body.Position ?? 0, body.ParentId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateBlock(Guid blockId, UpdateBlockRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateBlockCommand(blockId, body.Type, body.Properties));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteBlock(Guid blockId, ISender sender)
    {
        var result = await sender.Send(new DeleteBlockCommand(blockId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ReorderBlocks(ReorderBlocksRequest body, ISender sender)
    {
        var result = await sender.Send(new ReorderBlocksCommand(
            body.PageId,
            body.Items.Select(item => new ReorderBlockItem(item.BlockId, item.Position, item.ParentId)).ToList()
        ));
        return result.ToApiResult();
    }

    private static async Task<IResult> BatchUpdateBlocks(Guid pageId, BatchUpdateBlocksRequest body, ISender sender)
    {
        var result = await sender.Send(new BatchUpdateBlocksCommand(
            pageId,
            body.Blocks.Select(block => new BatchUpdateBlockItem(block.Id, block.Type, block.Properties, block.Position, block.ParentId)).ToList()
        ));
        return result.ToApiResult();
    }
}

public record CreatePageRequest(string Title, Guid? ParentId = null);
public record UpdatePageRequest(string? Title, string? IconType, string? IconValue, string? CoverUrl);
public record CreateBlockRequest(string Type, string? Properties = null, double? Position = null, Guid? ParentId = null);
public record UpdateBlockRequest(string? Type, string? Properties = null);
public record ReorderBlocksRequest(Guid PageId, List<ReorderBlockRequestItem> Items);
public record ReorderBlockRequestItem(Guid BlockId, double Position, Guid? ParentId = null);
public record BatchUpdateBlocksRequest(List<BatchUpdateBlockRequestItem> Blocks);
public record BatchUpdateBlockRequestItem(Guid Id, string? Type, string? Properties, double? Position, Guid? ParentId);
