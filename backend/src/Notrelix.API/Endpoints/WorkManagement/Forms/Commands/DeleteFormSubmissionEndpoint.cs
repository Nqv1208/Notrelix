using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.DeleteFormSubmission;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class DeleteFormSubmissionEndpoint
{
    public static IEndpointRouteBuilder MapDeleteFormSubmission(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.Forms.DeleteSubmission")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Delete a form submission");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid submissionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteFormSubmissionCommand(submissionId), cancellationToken);
        return result.ToNoContentResult();
    }
}
