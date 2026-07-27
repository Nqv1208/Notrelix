using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RestoreForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class RestoreFormEndpoint
{
    public static IEndpointRouteBuilder MapRestoreForm(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/restore", HandleAsync)
            .WithName("WorkManagement.Forms.Restore")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Restore a soft-deleted form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreFormCommand(formId), cancellationToken);
        return result.ToNoContentResult();
    }
}
