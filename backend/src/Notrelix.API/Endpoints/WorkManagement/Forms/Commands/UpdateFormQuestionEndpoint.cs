using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.UpdateFormQuestion;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class UpdateFormQuestionEndpoint
{
    public static IEndpointRouteBuilder MapUpdateFormQuestion(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/questions/{questionId:guid}", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.UpdateQuestion")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Update a form question");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        Guid questionId,
        UpdateFormQuestionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateFormQuestionCommand(
            formId,
            questionId,
            body.Label,
            body.IsRequired,
            body.ConfigJson,
            body.Position,
            body.BoardFieldId), cancellationToken);
        return result.ToNoContentResult();
    }
}
