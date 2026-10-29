using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class SubmitFormEndpoint
{
    public static IEndpointRouteBuilder MapSubmitForm(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/submissions", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.Submit")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Submit answers to a published form as the signed-in member");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        SubmitFormRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SubmitFormCommand(formId, body.Answers), cancellationToken);
        return result.ToCreatedResult();
    }
}
