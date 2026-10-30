using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;
using Notrelix.API.Endpoints.WorkManagement.Checklists.Queries;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklistItem;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists;

public static class MapChecklistEndpoints
{
    public static IEndpointRouteBuilder RegisterChecklistEndpoints(this IEndpointRouteBuilder app)
    {
        var itemGroup = app
            .MapGroup("/api/v1/board-items/{itemId:guid}/checklists")
            .WithTags("WorkManagement.Checklists")
            .WithOpenApi();

        itemGroup.MapGetChecklists();
        itemGroup.MapCreateChecklist();
        itemGroup.MapCopyChecklist();

        var boardGroup = app
            .MapGroup("/api/v1/boards/{boardId:guid}/checklists")
            .WithTags("WorkManagement.Checklists")
            .WithOpenApi();

        boardGroup.MapListBoardChecklists();

        var clGroup = app
            .MapGroup("/api/v1/checklists/{checklistId:guid}")
            .WithTags("WorkManagement.Checklists")
            .WithOpenApi();

        clGroup.MapUpdateChecklist();
        clGroup.MapDeleteChecklist();
        clGroup.MapResourcePost("/items", HandleCreateChecklistItem)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.CreateItemByChecklist")
            .WithSummary("Create a checklist item");
        clGroup.MapResourcePost("/move", HandleMoveChecklist)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.Move")
            .WithSummary("Move a checklist relative to its neighbors");

        var clItemGroup = app
            .MapGroup("/api/v1/checklist-items/{itemId:guid}")
            .WithTags("WorkManagement.Checklists")
            .WithOpenApi();

        clItemGroup.MapUpdateChecklistItem();
        clItemGroup.MapDeleteChecklistItem();
        clItemGroup.MapToggleChecklistItem();
        clItemGroup.MapResourcePost("/move", HandleMoveChecklistItem)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.MoveItem")
            .WithSummary("Move a checklist item relative to its neighbors");

        return app;
    }

    private static async Task<IResult> HandleCreateChecklistItem(
        Guid checklistId,
        CreateChecklistItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateChecklistItemCommand(checklistId, body.Title, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> HandleMoveChecklist(
        Guid checklistId,
        MoveChecklistRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveChecklistCommand(checklistId, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleMoveChecklistItem(
        Guid itemId,
        MoveChecklistItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveChecklistItemCommand(itemId, body.ExpectedVersion, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return result.ToNoContentResult();
    }
}
