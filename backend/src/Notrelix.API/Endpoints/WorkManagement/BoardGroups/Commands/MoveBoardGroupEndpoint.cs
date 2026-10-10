using Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.MoveBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class MoveBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapMoveBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/move", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardGroups.Move")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Move a group relative to its neighbors");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        MoveBoardGroupRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MoveBoardGroupCommand(groupId, body.PreviousSiblingId, body.NextSiblingId), cancellationToken);
        return result.ToNoContentResult();
    }
}
