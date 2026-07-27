using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.PublishForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class PublishFormEndpoint
{
    public static IEndpointRouteBuilder MapPublishForm(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/publish", HandleAsync)
            .WithName("WorkManagement.Forms.Publish")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Publish a form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishFormCommand(formId), cancellationToken);
        return result.ToNoContentResult();
    }
}
