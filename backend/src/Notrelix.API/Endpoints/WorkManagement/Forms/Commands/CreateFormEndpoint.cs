using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.CreateForm;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class CreateFormEndpoint
{
    public static IEndpointRouteBuilder MapCreateForm(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Forms.Create")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Create a new form");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateFormRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateFormCommand(boardId, body.Title), cancellationToken);
        return result.ToCreatedResult();
    }
}
