using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.ReopenFormSubmission;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class ReopenFormSubmissionEndpoint
{
    public static IEndpointRouteBuilder MapReopenFormSubmission(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/reopen", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.ReopenSubmission")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Return a rejected, spam or deleted form submission to the new responses");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid submissionId,
        bool? clearItem,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReopenFormSubmissionCommand(submissionId, clearItem == true), cancellationToken);
        return result.ToNoContentResult();
    }
}
