using Notrelix.API.Contracts.WorkManagement.Labels.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.CreateLabel;

namespace Notrelix.API.Endpoints.WorkManagement.Labels.Commands;

public static class CreateLabelEndpoint
{
    public static IEndpointRouteBuilder MapCreateLabel(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("WorkManagement.Labels.Create")
            .WithTags("WorkManagement.Labels")
            .WithSummary("Create a new label");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateLabelRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateLabelCommand(boardId, body.Color, body.Name), cancellationToken);
        return result.ToCreatedResult();
    }
}

