using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;
using Notrelix.Application.Features.Boards.Queries;

namespace Notrelix.API.Endpoints.Boards;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Workspace-scoped routes ──────────────────────────────
        var wsGroup = app
            .MapGroup("/api/workspaces/{slug}/boards")
            .WithTags("Boards")
            .RequireAuthorization()
            .WithOpenApi();

        wsGroup.MapGet("/", GetBoardsInWorkspace)
            .WithName("GetBoardsInWorkspace")
            .WithSummary("Get all boards in a workspace");

        wsGroup.MapPost("/", CreateBoardInWorkspace)
            .WithName("CreateBoardInWorkspace")
            .WithSummary("Create a new board in workspace");

        // ── Board-scoped routes ──────────────────────────────────
        var group = app
            .MapGroup("/api/boards")
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

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────

    private static async Task<IResult> GetBoardsInWorkspace(
        string slug,
        ISender sender)
    {
        var result = await sender.Send(new GetBoardsBySlugQuery(slug));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateBoardInWorkspace(
        string slug,
        CreateBoardInWorkspaceRequest body,
        ISender sender)
    {
        var result = await sender.Send(new CreateBoardBySlugCommand(slug, body.Title, body.Description, body.Background, body.Visibility));
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
        return Results.Ok(result);
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
}

public record CreateBoardInWorkspaceRequest(string Title, string? Description = null, string? Background = null, string? Visibility = null);
