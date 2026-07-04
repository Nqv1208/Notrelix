using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.API.Endpoints.WorkManagement.Boards.Commands;
using Notrelix.API.Endpoints.WorkManagement.Boards.Queries;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.AddBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RemoveBoardMember;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardMembers;

namespace Notrelix.API.Endpoints.WorkManagement.Boards;

public static class MapBoardEndpoints
{
    public static IEndpointRouteBuilder RegisterWorkManagementBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var wsGroup = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/boards")
            .RequireAuthorization()
            .WithTags("WorkManagement.Boards")
            .WithOpenApi();

        wsGroup.MapListWorkspaceBoards();
        wsGroup.MapCreateBoard();

        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}")
            .RequireAuthorization()
            .WithTags("WorkManagement.Boards")
            .WithOpenApi();

        boardGroup.MapGetBoard();
        boardGroup.MapRenameBoard();
        boardGroup.MapGetBoardOverview();
        boardGroup.MapPost("/archive", HandleArchiveBoard)
            .WithName("WorkManagement.Boards.Archive")
            .WithSummary("Archive a board");
        boardGroup.MapPost("/unarchive", HandleUnarchiveBoard)
            .WithName("WorkManagement.Boards.Unarchive")
            .WithSummary("Unarchive a board");

        var members = app
            .MapGroup("/api/v1/boards/{boardId:guid}/members")
            .RequireAuthorization()
            .WithTags("WorkManagement.Boards")
            .WithOpenApi();

        members.MapGet("/", HandleGetBoardMembers)
            .WithName("WorkManagement.Boards.GetMembers")
            .WithSummary("Get board members");
        members.MapPost("/", HandleAddBoardMember)
            .WithName("WorkManagement.Boards.AddMember")
            .WithSummary("Add a member to board");
        members.MapDelete("/{userId:guid}", HandleRemoveBoardMember)
            .WithName("WorkManagement.Boards.RemoveMember")
            .WithSummary("Remove a member from board");

        return app;
    }

    private static async Task<IResult> HandleArchiveBoard(
        HttpContext httpContext,
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var workspaceId = httpContext.GetWorkspaceIdHint();
        var result = await sender.Send(new ArchiveBoardCommand(workspaceId, boardId), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleUnarchiveBoard(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveBoardCommand(boardId), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleGetBoardMembers(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardMembersQuery(workspaceId, boardId), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> HandleAddBoardMember(
        Guid boardId,
        AddBoardMemberRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddBoardMemberCommand(boardId, body.UserId, body.Role is not null ? Enum.Parse<BoardRole>(body.Role, ignoreCase: true) : null), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleRemoveBoardMember(
        Guid boardId,
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveBoardMemberCommand(boardId, userId), cancellationToken);
        return result.ToNoContentResult();
    }
}

