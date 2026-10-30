using Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class DuplicateBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapDuplicateBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/duplicate", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardGroups.Duplicate")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Duplicate a group with its active items and their sub-items");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        DuplicateBoardGroupRequest? body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DuplicateBoardGroupCommand(groupId, body?.Title), cancellationToken);
        return result.ToCreatedResult();
    }
}
