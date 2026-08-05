using Notrelix.API.Contracts.WorkManagement.Templates.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;

namespace Notrelix.API.Endpoints.WorkManagement.Templates.Commands;

public static class CreateBoardTemplateEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardTemplate(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Templates.Create")
            .WithTags("WorkManagement.Templates")
            .WithSummary("Create a board template");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateBoardTemplateRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateBoardTemplateCommand(boardId, body.Name, body.Description),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
