using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;
using Notrelix.API.Endpoints.WorkManagement.Checklists.Queries;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

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

        var clGroup = app
            .MapGroup("/api/v1/checklists/{checklistId:guid}")
            .WithTags("WorkManagement.Checklists")
            .WithOpenApi();

        clGroup.MapUpdateChecklist();
        clGroup.MapDeleteChecklist();
        clGroup.MapResourcePost("/items", HandleCreateChecklistItem)
            .WithName("WorkManagement.Checklists.CreateItem")
            .WithSummary("Create a checklist item");

        var clItemGroup = app
            .MapGroup("/api/v1/checklist-items/{itemId:guid}")
            .WithTags("WorkManagement.Checklists")
            .WithOpenApi();

        clItemGroup.MapUpdateChecklistItem();
        clItemGroup.MapDeleteChecklistItem();

        return app;
    }

    private static async Task<IResult> HandleCreateChecklistItem(
        Guid checklistId,
        CreateChecklistItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateChecklistItemCommand(checklistId, body.Title), cancellationToken);
        return result.ToCreatedResult();
    }
}
