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
using Notrelix.Application.Features.Workspaces.Commands.CancelInvitation;
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
using Notrelix.Application.Features.Workspaces.Queries.GetInvitationByToken;
using Notrelix.Application.Features.Workspaces.Queries.GetUserPendingInvitations;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Interfaces;
namespace Notrelix.API.Endpoints.Workspaces;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces")
            .WithTags("Workspaces")
            .RequireAuthorization()
            .WithOpenApi();

        // ── Workspace CRUD ───────────────────────────────────────
        group.MapPost("/", CreateWorkspace)
            .WithName("CreateWorkspace")
            .WithSummary("Create a new workspace");

        group.MapGet("/", GetUserWorkspaces)
            .WithName("GetUserWorkspaces")
            .WithSummary("Get current user's workspaces");

        group.MapGet("/{workspaceId:guid}", GetWorkspaceById)
            .WithName("GetWorkspace")
            .WithSummary("Get workspace by ID");

        group.MapGet("/by-slug/{slug}", GetWorkspaceBySlug)
            .WithName("GetWorkspaceBySlug")
            .WithSummary("Resolve a workspace by slug for legacy/deep-link migration");

        group.MapPatch("/{workspaceId:guid}", UpdateWorkspace)
            .WithName("UpdateWorkspace")
            .WithSummary("Update workspace settings");

        group.MapDelete("/{workspaceId:guid}", ArchiveWorkspace)
            .WithName("ArchiveWorkspace")
            .WithSummary("Archive (soft delete) a workspace");

        // ── Members ──────────────────────────────────────────────
        group.MapGet("/{workspaceId:guid}/members", GetMembers)
            .WithName("GetWorkspaceMembers")
            .WithSummary("Get workspace members");

        group.MapPatch("/{workspaceId:guid}/members/{userId:guid}", UpdateMemberRole)
            .WithName("UpdateMemberRole")
            .WithSummary("Update a member's role");

        group.MapDelete("/{workspaceId:guid}/members/{userId:guid}", RemoveMember)
            .WithName("RemoveMember")
            .WithSummary("Remove a member from workspace");

        // ── Invitations ──────────────────────────────────────────
        group.MapPost("/{workspaceId:guid}/invitations", InviteMember)
            .WithName("InviteMember")
            .WithSummary("Invite a member to workspace");

        group.MapGet("/{workspaceId:guid}/invitations", GetInvitations)
            .WithName("GetWorkspaceInvitations")
            .WithSummary("Get workspace invitations");

        group.MapDelete("/{workspaceId:guid}/invitations/{invitationId:guid}", CancelInvitation)
            .WithName("CancelInvitation")
            .WithSummary("Cancel a workspace invitation");

        group.MapGet("/invitations/by-token/{token}", GetInvitationByToken)
            .WithName("GetInvitationByToken")
            .WithSummary("Get workspace invitation details by token")
            .AllowAnonymous();

        group.MapPost("/invitations/accept/{token}", AcceptInvitation)
            .WithName("AcceptInvitation")
            .WithSummary("Accept a workspace invitation by token");

        group.MapGet("/invitations/pending", GetPendingInvitations)
            .WithName("GetUserPendingInvitations")
            .WithSummary("Get pending invitations for the current logged-in user");

        // ── Activity ─────────────────────────────────────────────
        group.MapGet("/{workspaceId:guid}/activity", GetActivity)
            .WithName("GetWorkspaceActivity")
            .WithSummary("Get workspace activity log");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────

    private static async Task<IResult> CreateWorkspace(
        CreateWorkspaceCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetUserWorkspaces(
        ISender sender,
        ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            return Results.Unauthorized();

        var result = await sender.Send(new GetUserWorkspacesQuery(currentUser.UserId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetWorkspaceById(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetWorkspaceBySlug(
        string slug,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceBySlugQuery(slug));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateWorkspace(
        Guid workspaceId,
        UpdateWorkspaceCommand command,
        ISender sender)
    {
        var result = await sender.Send(command with { WorkspaceId = workspaceId });
        return result.ToApiResult();
    }

    private static async Task<IResult> ArchiveWorkspace(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveWorkspaceCommand(workspaceId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> GetMembers(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceMembersQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateMemberRole(
        Guid workspaceId,
        Guid userId,
        UpdateMemberRoleRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateMemberRoleCommand(workspaceId, userId, body.Role));
        return result.ToApiResult();
    }

    private static async Task<IResult> RemoveMember(
        Guid workspaceId,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new RemoveMemberCommand(workspaceId, userId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> InviteMember(
        Guid workspaceId,
        InviteMemberRequest body,
        ISender sender)
    {
        var result = await sender.Send(new InviteMemberCommand(workspaceId, body.Email, body.Role));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetInvitations(
        Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceInvitationsQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CancelInvitation(
        Guid workspaceId,
        Guid invitationId,
        ISender sender)
    {
        var result = await sender.Send(new CancelInvitationCommand(workspaceId, invitationId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> GetActivity(
        Guid workspaceId,
        ISender sender,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetWorkspaceActivityQuery(workspaceId, page, pageSize));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetInvitationByToken(
        string token,
        ISender sender)
    {
        var result = await sender.Send(new GetInvitationByTokenQuery(token));
        return result.ToApiResult();
    }

    private static async Task<IResult> AcceptInvitation(
        string token,
        ISender sender)
    {
        var result = await sender.Send(new AcceptInvitationCommand(token));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetPendingInvitations(
        ISender sender)
    {
        var result = await sender.Send(new GetUserPendingInvitationsQuery());
        return result.ToApiResult();
    }
}

// ── Request DTOs for route-bound endpoints ─────────────────────
public record UpdateMemberRoleRequest(string Role);
public record InviteMemberRequest(string Email, string Role);
