using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Boards.Commands;
using Notrelix.Application.Features.Boards.Queries;

namespace Notrelix.API.Endpoints.Checklists;

public static class ChecklistEndpoints
{
    public static IEndpointRouteBuilder MapChecklistEndpoints(this IEndpointRouteBuilder app)
    {
        // Card-scoped
        var cardGroup = app
            .MapGroup("/api/cards/{cardId:guid}/checklists")
            .WithTags("Checklists")
            .RequireAuthorization()
            .WithOpenApi();

        cardGroup.MapGet("/", GetChecklists)
            .WithName("GetChecklists");

        cardGroup.MapPost("/", CreateChecklist)
            .WithName("CreateChecklist");

        // Checklist-scoped
        var clGroup = app
            .MapGroup("/api/checklists")
            .WithTags("Checklists")
            .RequireAuthorization()
            .WithOpenApi();

        clGroup.MapPatch("/{checklistId:guid}", UpdateChecklist)
            .WithName("UpdateChecklist");

        clGroup.MapDelete("/{checklistId:guid}", DeleteChecklist)
            .WithName("DeleteChecklist");

        clGroup.MapPost("/{checklistId:guid}/items", CreateChecklistItem)
            .WithName("CreateChecklistItem");

        // ChecklistItem-scoped
        var itemGroup = app
            .MapGroup("/api/checklist-items")
            .WithTags("Checklists")
            .RequireAuthorization()
            .WithOpenApi();

        itemGroup.MapPatch("/{itemId:guid}", UpdateChecklistItem)
            .WithName("UpdateChecklistItem");

        itemGroup.MapDelete("/{itemId:guid}", DeleteChecklistItem)
            .WithName("DeleteChecklistItem");

        return app;
    }

    private static async Task<IResult> GetChecklists(Guid cardId, ISender sender)
    {
        var result = await sender.Send(new GetChecklistsQuery(cardId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateChecklist(Guid cardId, CreateChecklistRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateChecklistCommand(cardId, body.Title));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateChecklist(Guid checklistId, UpdateChecklistRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateChecklistCommand(checklistId, body.Title, body.Position));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteChecklist(Guid checklistId, ISender sender)
    {
        var result = await sender.Send(new DeleteChecklistCommand(checklistId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> CreateChecklistItem(Guid checklistId, CreateChecklistItemRequest body, ISender sender)
    {
        var result = await sender.Send(new CreateChecklistItemCommand(checklistId, body.Title));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateChecklistItem(Guid itemId, UpdateChecklistItemRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateChecklistItemCommand(itemId, body.Title, body.IsChecked, body.DueDate, body.AssigneeId));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteChecklistItem(Guid itemId, ISender sender)
    {
        var result = await sender.Send(new DeleteChecklistItemCommand(itemId));
        return result.ToNoContentResult();
    }
}

public record CreateChecklistRequest(string Title = "Checklist");
public record UpdateChecklistRequest(string? Title, double? Position);
public record CreateChecklistItemRequest(string Title);
public record UpdateChecklistItemRequest(string? Title, bool? IsChecked, DateTime? DueDate, Guid? AssigneeId);
