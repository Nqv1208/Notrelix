using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.AddFormQuestion;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class AddFormQuestionEndpoint
{
    public static IEndpointRouteBuilder MapAddFormQuestion(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/questions", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.AddQuestion")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Add a question to a form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        AddFormQuestionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddFormQuestionCommand(
            formId,
            body.QuestionKey,
            body.QuestionType,
            body.Label,
            body.IsRequired,
            body.ConfigJson,
            body.Position), cancellationToken);
        return result.ToNoContentResult();
    }
}
