using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormDetails;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class UpdateFormDetailsEndpoint
{
    public static IEndpointRouteBuilder MapUpdateFormDetails(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithName("WorkManagement.Forms.UpdateDetails")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Update form details");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        UpdateFormDetailsRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateFormDetailsCommand(formId, body.Title, default, "{}", "{}"), cancellationToken);
        return result.ToNoContentResult();
    }
}
