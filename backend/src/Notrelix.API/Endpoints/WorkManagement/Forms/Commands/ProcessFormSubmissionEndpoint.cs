using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.ProcessFormSubmission;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class ProcessFormSubmissionEndpoint
{
    public static IEndpointRouteBuilder MapProcessFormSubmission(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/process", HandleAsync)
            .WithName("WorkManagement.Forms.ProcessSubmission")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Process a form submission");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid submissionId,
        ProcessFormSubmissionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ProcessFormSubmissionCommand(submissionId, body.CreatedItemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
