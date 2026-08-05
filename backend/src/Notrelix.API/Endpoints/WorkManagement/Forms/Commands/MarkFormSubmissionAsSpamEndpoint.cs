using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.MarkFormSubmissionAsSpam;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class MarkFormSubmissionAsSpamEndpoint
{
    public static IEndpointRouteBuilder MapMarkFormSubmissionAsSpam(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/spam", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.MarkAsSpam")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Mark a form submission as spam");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid submissionId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkFormSubmissionAsSpamCommand(submissionId), cancellationToken);
        return result.ToNoContentResult();
    }
}
