using Notrelix.API.Contracts.WorkManagement.Labels.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;

namespace Notrelix.API.Endpoints.WorkManagement.Labels.Commands;

public static class UpdateLabelEndpoint
{
    public static IEndpointRouteBuilder MapUpdateLabel(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithName("WorkManagement.Labels.Update")
            .WithTags("WorkManagement.Labels")
            .WithSummary("Update label name or color");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid labelId,
        UpdateLabelRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateLabelCommand(labelId, body.Name, body.Color), cancellationToken);
        return result.ToApiResult();
    }
}

