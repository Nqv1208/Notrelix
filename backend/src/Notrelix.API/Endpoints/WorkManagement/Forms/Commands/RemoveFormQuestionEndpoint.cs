using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.RemoveFormQuestion;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class RemoveFormQuestionEndpoint
{
    public static IEndpointRouteBuilder MapRemoveFormQuestion(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/questions/{questionId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.RemoveQuestion")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Remove a question from a form");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid formId, Guid questionId, [FromQuery] long expectedVersion, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFormQuestionCommand(formId, questionId, expectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
