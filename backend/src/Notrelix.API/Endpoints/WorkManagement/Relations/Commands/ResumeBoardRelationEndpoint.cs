using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Relations.Commands.ResumeBoardRelation;

namespace Notrelix.API.Endpoints.WorkManagement.Relations.Commands;

public static class ResumeBoardRelationEndpoint
{
    public static IEndpointRouteBuilder MapResumeBoardRelation(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/resume", HandleAsync)
            .WithName("WorkManagement.Relations.Resume")
            .WithTags("WorkManagement.Relations")
            .WithSummary("Resume a board relation");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid relationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResumeBoardRelationCommand(relationId), cancellationToken);
        return result.ToApiResult();
    }
}
