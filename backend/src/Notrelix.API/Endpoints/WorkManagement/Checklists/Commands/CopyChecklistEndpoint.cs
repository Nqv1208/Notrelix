using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CopyChecklist;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class CopyChecklistEndpoint
{
    public static IEndpointRouteBuilder MapCopyChecklist(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/copy", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.Copy")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Create a checklist that copies the items of another checklist of the board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        CopyChecklistRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CopyChecklistCommand(itemId, body.SourceChecklistId, body.Title), cancellationToken);
        return result.ToCreatedResult();
    }
}
