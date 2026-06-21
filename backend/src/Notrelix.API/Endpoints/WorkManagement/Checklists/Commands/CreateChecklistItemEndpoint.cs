using MediatR;
using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class CreateChecklistItemEndpoint
{
    public static IEndpointRouteBuilder MapCreateChecklistItem(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("WorkManagement.Checklists.CreateItem")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Create a checklist item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid checklistId,
        CreateChecklistItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateChecklistItemCommand(checklistId, body.Title), cancellationToken);
        return result.ToCreatedResult();
    }
}

