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
namespace Notrelix.API.Endpoints.Boards;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Workspace-scoped routes ──────────────────────────────
        var wsByIdGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/boards")
            .WithTags("Boards")
            .RequireAuthorization()
            .WithOpenApi();

        wsByIdGroup.MapGet("/", GetBoardsInWorkspaceById)
            .WithName("GetBoardsInWorkspaceById")
            .WithSummary("Get all boards in a workspace by workspace ID");

        wsByIdGroup.MapPost("/", CreateBoardInWorkspace)
            .WithName("CreateBoardInWorkspace")
            .WithSummary("Create a new board in workspace");

        // ── Board-scoped routes ──────────────────────────────────
        var group = app
            .MapGroup("/api/v1/boards")
            .WithTags("Boards")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/{boardId:guid}", GetBoard)
            .WithName("GetBoard")
            .WithSummary("Get board by ID");

        group.MapGet("/{boardId:guid}/full", GetFullBoard)
            .WithName("GetFullBoard")
            .WithSummary("Get full board with lists and cards");

        group.MapPatch("/{boardId:guid}", UpdateBoard)
            .WithName("UpdateBoard")
            .WithSummary("Update board settings");

        group.MapDelete("/{boardId:guid}", DeleteBoard)
            .WithName("DeleteBoard")
            .WithSummary("Soft delete a board");

        group.MapPost("/{boardId:guid}/archive", ArchiveBoard)
            .WithName("ArchiveBoard")
            .WithSummary("Archive a board");

        group.MapPost("/{boardId:guid}/unarchive", UnarchiveBoard)
            .WithName("UnarchiveBoard")
            .WithSummary("Unarchive a board");

        group.MapPost("/{boardId:guid}/columns", CreateColumn)
            .WithName("CreateBoardColumn")
            .WithSummary("Create a board table column");

        group.MapPatch("/{boardId:guid}/columns/{columnId:guid}", UpdateColumn)
            .WithName("UpdateBoardColumn")
            .WithSummary("Update a board table column");

        group.MapDelete("/{boardId:guid}/columns/{columnId:guid}", DeleteColumn)
            .WithName("DeleteBoardColumn")
            .WithSummary("Delete a board table column");

        group.MapPost("/{boardId:guid}/columns/reorder", ReorderColumns)
            .WithName("ReorderBoardColumns")
            .WithSummary("Reorder board table columns");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────

    private static async Task<IResult> GetBoardsInWorkspaceById(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetBoardsQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateBoardInWorkspace(
        Guid workspaceId,
        CreateBoardInWorkspaceRequest body,
        ISender sender)
    {
        var result = await sender.Send(new CreateBoardInWorkspaceCommand(workspaceId, body.Title, body.Description, body.Background, body.Visibility));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetBoard(
        Guid boardId,
        ISender sender)
    {
        var result = await sender.Send(new GetBoardQuery(boardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetFullBoard(
        Guid boardId,
        ISender sender)
    {
        var result = await sender.Send(new Notrelix.Application.Features.Boards.Queries.GetFullBoard.GetFullBoardQuery(boardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateBoard(
        Guid boardId,
        UpdateBoardCommand command,
        ISender sender)
    {
        // Override boardId from route
        var cmd = command with { BoardId = boardId };
        var result = await sender.Send(cmd);
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteBoard(
        Guid boardId,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveBoardCommand(boardId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ArchiveBoard(
        Guid boardId,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveBoardCommand(boardId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> UnarchiveBoard(
        Guid boardId,
        ISender sender)
    {
        var result = await sender.Send(new UnarchiveBoardCommand(boardId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> CreateColumn(
        Guid boardId,
        CreateBoardColumnRequest body,
        ISender sender)
    {
        var result = await sender.Send(new CreateBoardColumnCommand(boardId, body.Name, body.FieldType, body.Settings, body.Position));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateColumn(
        Guid boardId,
        Guid columnId,
        UpdateBoardColumnRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateBoardColumnCommand(boardId, columnId, body.Name, body.FieldType, body.Settings, body.IsHidden));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteColumn(
        Guid boardId,
        Guid columnId,
        ISender sender)
    {
        var result = await sender.Send(new DeleteBoardColumnCommand(boardId, columnId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ReorderColumns(
        Guid boardId,
        ReorderBoardColumnsCommand command,
        ISender sender)
    {
        var result = await sender.Send(command with { BoardId = boardId });
        return result.ToNoContentResult();
    }
}

public record CreateBoardInWorkspaceRequest(string Title, string? Description = null, string? Background = null, string? Visibility = null);
public record CreateBoardColumnRequest(string Name, string FieldType, string? Settings = null, double? Position = null);
public record UpdateBoardColumnRequest(string? Name = null, string? FieldType = null, string? Settings = null, bool? IsHidden = null);
