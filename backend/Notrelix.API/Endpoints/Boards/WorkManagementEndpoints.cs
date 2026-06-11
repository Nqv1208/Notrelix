using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Commands;
using Notrelix.Application.Features.WorkManagement.Queries;

namespace Notrelix.API.Endpoints.Boards;

public static class WorkManagementEndpoints
{
    public static IEndpointRouteBuilder MapWorkManagementEndpoints(this IEndpointRouteBuilder app)
    {
        // Nhóm API boards
        var boardsGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}")
            .WithTags("Work Management")
            .RequireAuthorization()
            .WithOpenApi();

        // GET /api/v1/boards/{boardId}/schema
        boardsGroup.MapGet("/schema", GetBoardSchema)
            .WithName("GetBoardSchema")
            .WithSummary("Get schema (fields, groups) of a board");

        // GET /api/v1/boards/{boardId}/items
        boardsGroup.MapGet("/items", GetBoardItems)
            .WithName("GetBoardItems")
            .WithSummary("Get all items of a board");

        // POST /api/v1/boards/{boardId}/fields
        boardsGroup.MapPost("/fields", CreateBoardField)
            .WithName("CreateBoardField")
            .WithSummary("Create a new field in a board");

        // PATCH /api/v1/boards/{boardId}/fields/{fieldId:guid}
        boardsGroup.MapPatch("/fields/{fieldId:guid}", UpdateBoardField)
            .WithName("UpdateBoardField")
            .WithSummary("Update details or settings of a board field");

        // DELETE /api/v1/boards/{boardId}/fields/{fieldId:guid}
        boardsGroup.MapDelete("/fields/{fieldId:guid}", DeleteBoardField)
            .WithName("DeleteBoardField")
            .WithSummary("Delete a field from a board");

        // POST /api/v1/boards/{boardId}/items
        boardsGroup.MapPost("/items", CreateBoardItem)
            .WithName("CreateBoardItem")
            .WithSummary("Create a new item in a board group");

        // PATCH /api/v1/boards/{boardId}/items/{itemId:guid}/values/{fieldId:guid}
        boardsGroup.MapPatch("/items/{itemId:guid}/values/{fieldId:guid}", UpdateBoardItemFieldValue)
            .WithName("UpdateBoardItemFieldValue")
            .WithSummary("Update cell value of a board item");

        // PATCH /api/v1/boards/{boardId}/items/{itemId:guid}/move
        boardsGroup.MapPatch("/items/{itemId:guid}/move", MoveBoardItem)
            .WithName("MoveBoardItem")
            .WithSummary("Move item to another group or change position");

        // POST /api/v1/boards/{boardId}/views
        boardsGroup.MapPost("/views", CreateBoardView)
            .WithName("CreateBoardView")
            .WithSummary("Create a new saved view config for a board");

        // PATCH /api/v1/boards/{boardId}/views/{viewId:guid}
        boardsGroup.MapPatch("/views/{viewId:guid}", UpdateBoardViewConfig)
            .WithName("UpdateBoardViewConfig")
            .WithSummary("Update configuration of a board view");

        return app;
    }

    private static async Task<IResult> GetBoardSchema(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetBoardSchemaQuery(workspaceId, boardId));
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBoardItems(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender)
    {
        var result = await sender.Send(new GetBoardItemsQuery(workspaceId, boardId));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateBoardField(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        CreateBoardFieldRequest body,
        ISender sender)
    {
        var result = await sender.Send(new CreateBoardFieldCommand(
            workspaceId,
            boardId,
            body.Name,
            body.Type,
            body.SettingsJson ?? "{}",
            body.Position));
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateBoardField(
        Guid boardId,
        Guid fieldId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        UpdateBoardFieldRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateBoardFieldCommand(
            workspaceId,
            boardId,
            fieldId,
            body.Name,
            body.Type,
            body.SettingsJson ?? "{}"));
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteBoardField(
        Guid boardId,
        Guid fieldId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender)
    {
        await sender.Send(new DeleteBoardFieldCommand(workspaceId, boardId, fieldId));
        return Results.NoContent();
    }

    private static async Task<IResult> CreateBoardItem(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        CreateBoardItemRequest body,
        ISender sender)
    {
        var result = await sender.Send(new CreateBoardItemCommand(
            workspaceId,
            boardId,
            body.GroupId,
            body.Title,
            body.Position));
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateBoardItemFieldValue(
        Guid boardId,
        Guid itemId,
        Guid fieldId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        UpdateBoardItemFieldValueRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateBoardItemFieldValueCommand(
            workspaceId,
            boardId,
            itemId,
            fieldId,
            body.Value));
        return Results.Ok(result);
    }

    private static async Task<IResult> MoveBoardItem(
        Guid boardId,
        Guid itemId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        MoveBoardItemRequest body,
        ISender sender)
    {
        var result = await sender.Send(new MoveBoardItemCommand(
            workspaceId,
            boardId,
            itemId,
            body.NewGroupId,
            body.Position));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateBoardView(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        CreateBoardViewRequest body,
        ISender sender)
    {
        var result = await sender.Send(new CreateBoardViewCommand(
            workspaceId,
            boardId,
            body.Name,
            body.ViewMode,
            body.Position));
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateBoardViewConfig(
        Guid boardId,
        Guid viewId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        UpdateBoardViewConfigRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateBoardViewConfigCommand(
            workspaceId,
            boardId,
            viewId,
            body.ConfigJson));
        return Results.Ok(result);
    }
}

// Requests models
public record CreateBoardFieldRequest(string Name, string Type, string? SettingsJson, double Position);
public record UpdateBoardFieldRequest(string Name, string Type, string? SettingsJson);
public record CreateBoardItemRequest(Guid GroupId, string Title, double Position);
public record UpdateBoardItemFieldValueRequest(object? Value);
public record MoveBoardItemRequest(Guid NewGroupId, double Position);
public record CreateBoardViewRequest(string Name, string ViewMode, double Position);
public record UpdateBoardViewConfigRequest(string ConfigJson);
