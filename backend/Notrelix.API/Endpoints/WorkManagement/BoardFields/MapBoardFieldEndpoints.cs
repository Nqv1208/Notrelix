using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Contracts.WorkManagement.BoardFields.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderBoardFields;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardSchema.Queries.GetBoardSchema;

namespace Notrelix.API.Endpoints.WorkManagement.BoardFields;

public static class MapBoardFieldEndpoints
{
    public static IEndpointRouteBuilder RegisterBoardFieldEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/boards/{boardId:guid}/fields")
            .RequireAuthorization()
            .WithTags("WorkManagement.BoardFields")
            .WithOpenApi();

        group.MapPost("/", HandleCreateBoardField)
            .WithName("WorkManagement.BoardFields.Create")
            .WithSummary("Create a new field in a board");
        group.MapPatch("/{fieldId:guid}", HandleUpdateBoardField)
            .WithName("WorkManagement.BoardFields.Update")
            .WithSummary("Update details or settings of a board field");
        group.MapDelete("/{fieldId:guid}", HandleDeleteBoardField)
            .WithName("WorkManagement.BoardFields.Delete")
            .WithSummary("Delete a field from a board");
        group.MapPost("/reorder", HandleReorderBoardFields)
            .WithName("WorkManagement.BoardFields.Reorder")
            .WithSummary("Reorder board fields");

        var schemaGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}")
            .RequireAuthorization()
            .WithTags("WorkManagement.BoardFields")
            .WithOpenApi();

        schemaGroup.MapGet("/schema", HandleGetBoardSchema)
            .WithName("WorkManagement.BoardFields.GetSchema")
            .WithSummary("Get schema (fields, groups) of a board");

        return app;
    }

    private static async Task<IResult> HandleCreateBoardField(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        CreateBoardFieldRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBoardFieldCommand(
            boardId,
            body.Name,
            body.Type,
            body.SettingsJson ?? "{}",
            body.Position.ToString()), cancellationToken);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> HandleUpdateBoardField(
        Guid boardId,
        Guid fieldId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        UpdateBoardFieldRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardFieldCommand(
            boardId,
            fieldId,
            body.Name,
            body.Type,
            body.SettingsJson ?? "{}"), cancellationToken);
        return result.ToApiResult();
    }

    private static async Task<IResult> HandleDeleteBoardField(
        Guid boardId,
        Guid fieldId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteBoardFieldCommand(boardId, fieldId), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleReorderBoardFields(
        Guid boardId,
        ReorderBoardFieldsCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command with { BoardId = boardId }, cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleGetBoardSchema(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardSchemaQuery(workspaceId, boardId), cancellationToken);
        return Results.Ok(result);
    }
}

