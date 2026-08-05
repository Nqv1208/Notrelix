using Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class UpdateBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapUpdateBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardGroups.Update")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Update group title and color");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        UpdateBoardGroupRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateBoardGroupCommand(groupId, body.Title, body.Color), cancellationToken);
        return result.ToApiResult();
    }
}

