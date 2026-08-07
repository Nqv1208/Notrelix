using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Relations.Commands.DeleteBoardRelation;

namespace Notrelix.API.Endpoints.WorkManagement.Relations.Commands;

public static class DeleteBoardRelationEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardRelation(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Relations.Delete")
            .WithTags("WorkManagement.Relations")
            .WithSummary("Delete a board relation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid relationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardRelationCommand(relationId), cancellationToken);
        return result.ToNoContentResult();
    }
}
