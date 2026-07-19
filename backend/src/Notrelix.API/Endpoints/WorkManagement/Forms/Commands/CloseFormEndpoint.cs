using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CloseForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class CloseFormEndpoint
{
    public static IEndpointRouteBuilder MapCloseForm(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/close", HandleAsync)
            .WithName("WorkManagement.Forms.Close")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Close a form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CloseFormCommand(formId), cancellationToken);
        return result.ToNoContentResult();
    }
}
