using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.ArchiveBoardTemplate;

namespace Notrelix.API.Endpoints.WorkManagement.Templates.Commands;

public static class ArchiveBoardTemplateEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoardTemplate(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/archive", HandleAsync)
            .WithName("WorkManagement.Templates.Archive")
            .WithTags("WorkManagement.Templates")
            .WithSummary("Archive a board template");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid templateId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardTemplateCommand(templateId), cancellationToken);
        return result.ToApiResult();
    }
}
