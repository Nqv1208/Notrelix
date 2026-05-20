using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;

namespace Notrelix.API.Endpoints.Lists;

public static class ListEndpoints
{
    public static IEndpointRouteBuilder MapListEndpoints(this IEndpointRouteBuilder app)
    {
        // ── Board-scoped routes ──────────────────────────────────
        var boardGroup = app
            .MapGroup("/api/boards/{boardId:guid}/lists")
            .WithTags("Lists")
            .RequireAuthorization()
            .WithOpenApi();

        boardGroup.MapPost("/", CreateList)
            .WithName("CreateList")
            .WithSummary("Create a new list in board");

        boardGroup.MapPost("/reorder", ReorderLists)
            .WithName("ReorderLists")
            .WithSummary("Reorder lists in a board");

        // ── List-scoped routes ───────────────────────────────────
        var group = app
            .MapGroup("/api/lists")
            .WithTags("Lists")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPatch("/{listId:guid}", UpdateList)
            .WithName("UpdateList")
            .WithSummary("Update list title");

        group.MapDelete("/{listId:guid}", DeleteList)
            .WithName("DeleteList")
            .WithSummary("Soft delete a list");

        group.MapPost("/{listId:guid}/archive", ArchiveList)
            .WithName("ArchiveList")
            .WithSummary("Archive a list");

        group.MapPost("/{listId:guid}/unarchive", UnarchiveList)
            .WithName("UnarchiveList")
            .WithSummary("Unarchive a list");

        return app;
    }

    private static async Task<IResult> CreateList(Guid boardId, CreateListRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateListCommand(boardId, body.Title, body.Position));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateList(Guid listId, UpdateListRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateListCommand(listId, body.Title));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteList(Guid listId, ISender sender)
    {
        var result = await sender.Send(new ArchiveListCommand(listId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ArchiveList(Guid listId, ISender sender)
    {
        var result = await sender.Send(new ArchiveListCommand(listId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> UnarchiveList(Guid listId, ISender sender)
    {
        var result = await sender.Send(new UnarchiveListCommand(listId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> ReorderLists(Guid boardId, ReorderListsCommand command, ISender sender)
    {
        var cmd = command with { BoardId = boardId };
        var result = await sender.Send(cmd);
        return result.ToNoContentResult();
    }
}

public record CreateListRequest(string Title, double? Position = null);
public record UpdateListRequest(string Title);
