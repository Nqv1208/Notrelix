using Notrelix.API.Contracts.WorkManagement.Relations.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Relations.Commands.CreateBoardRelation;

namespace Notrelix.API.Endpoints.WorkManagement.Relations.Commands;

public static class CreateBoardRelationEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardRelation(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Relations.Create")
            .WithTags("WorkManagement.Relations")
            .WithSummary("Create a relation between two boards");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateBoardRelationRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateBoardRelationCommand(boardId, Guid.Parse(body.TargetBoardId), body.RelationType),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
