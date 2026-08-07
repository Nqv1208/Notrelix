using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RejectFormSubmission;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class RejectFormSubmissionEndpoint
{
    public static IEndpointRouteBuilder MapRejectFormSubmission(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/reject", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.RejectSubmission")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Reject a form submission");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid submissionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RejectFormSubmissionCommand(submissionId), cancellationToken);
        return result.ToNoContentResult();
    }
}
