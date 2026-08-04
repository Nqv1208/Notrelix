using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.PublishBoardTemplate;

namespace Notrelix.API.Endpoints.WorkManagement.Templates.Commands;

public static class PublishBoardTemplateEndpoint
{
    public static IEndpointRouteBuilder MapPublishBoardTemplate(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/publish", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Templates.Publish")
            .WithTags("WorkManagement.Templates")
            .WithSummary("Publish a board template");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid templateId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PublishBoardTemplateCommand(templateId), cancellationToken);
        return result.ToApiResult();
    }
}
