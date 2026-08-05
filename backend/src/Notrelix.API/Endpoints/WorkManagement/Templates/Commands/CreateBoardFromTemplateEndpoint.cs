using Notrelix.API.Contracts.WorkManagement.Templates.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;

namespace Notrelix.API.Endpoints.WorkManagement.Templates.Commands;

public static class CreateBoardFromTemplateEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardFromTemplate(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/create-board", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Templates.CreateBoard")
            .WithTags("WorkManagement.Templates")
            .WithSummary("Create a board from a template");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid templateId,
        CreateBoardFromTemplateRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateBoardFromTemplateCommand(templateId, Guid.Parse(body.WorkspaceId), body.Name),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
