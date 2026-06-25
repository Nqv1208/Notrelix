using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklist;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class UpdateChecklistEndpoint
{
    public static IEndpointRouteBuilder MapUpdateChecklist(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/", HandleAsync)
            .WithName("WorkManagement.Checklists.Update")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Update a checklist");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid checklistId,
        UpdateChecklistRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateChecklistCommand(checklistId), cancellationToken);
        return result.ToApiResult();
    }
}

