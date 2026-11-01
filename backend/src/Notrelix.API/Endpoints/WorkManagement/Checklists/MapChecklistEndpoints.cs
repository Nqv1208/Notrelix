using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;
using Notrelix.API.Endpoints.WorkManagement.Checklists.Queries;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.SetChecklistItemAssignee;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.SetChecklistItemDue;

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
        clItemGroup.MapResourcePut("/assignee", HandleSetChecklistItemAssignee)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.SetItemAssignee")
            .WithSummary("Give a checklist item to a person, or to nobody");
        clItemGroup.MapResourcePut("/due", HandleSetChecklistItemDue)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.SetItemDue")
            .WithSummary("Set or clear the due time of a checklist item");

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

    private static async Task<IResult> HandleSetChecklistItemAssignee(
        Guid itemId,
        SetChecklistItemAssigneeRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetChecklistItemAssigneeCommand(itemId, body.AssigneeUserId, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }

    private static async Task<IResult> HandleSetChecklistItemDue(
        Guid itemId,
        SetChecklistItemDueRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetChecklistItemDueCommand(itemId, body.DueAt, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
