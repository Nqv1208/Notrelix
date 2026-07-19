using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.DeleteBoardTemplate;

namespace Notrelix.API.Endpoints.WorkManagement.Templates.Commands;

public static class DeleteBoardTemplateEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardTemplate(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.Templates.Delete")
            .WithTags("WorkManagement.Templates")
            .WithSummary("Delete a board template");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid templateId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardTemplateCommand(templateId), cancellationToken);
        return result.ToNoContentResult();
    }
}
