using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SoftDeleteForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class SoftDeleteFormEndpoint
{
    public static IEndpointRouteBuilder MapSoftDeleteForm(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.Forms.SoftDelete")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Soft-delete a form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SoftDeleteFormCommand(formId), cancellationToken);
        return result.ToNoContentResult();
    }
}
