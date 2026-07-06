using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class DeleteChecklistEndpoint
{
    public static IEndpointRouteBuilder MapDeleteChecklist(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.Checklists.Delete")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Delete a checklist");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid checklistId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteChecklistCommand(checklistId), cancellationToken);
        return result.ToNoContentResult();
    }
}
