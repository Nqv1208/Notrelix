using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Relations.Commands.PauseBoardRelation;

namespace Notrelix.API.Endpoints.WorkManagement.Relations.Commands;

public static class PauseBoardRelationEndpoint
{
    public static IEndpointRouteBuilder MapPauseBoardRelation(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/pause", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Relations.Pause")
            .WithTags("WorkManagement.Relations")
            .WithSummary("Pause a board relation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid relationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PauseBoardRelationCommand(relationId), cancellationToken);
        return result.ToApiResult();
    }
}
