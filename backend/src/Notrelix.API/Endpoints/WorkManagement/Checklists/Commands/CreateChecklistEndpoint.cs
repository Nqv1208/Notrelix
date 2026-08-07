using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class CreateChecklistEndpoint
{
    public static IEndpointRouteBuilder MapCreateChecklist(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.Create")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Create a new checklist");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        CreateChecklistRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateChecklistCommand(itemId, body.Title), cancellationToken);
        return result.ToCreatedResult();
    }
}

