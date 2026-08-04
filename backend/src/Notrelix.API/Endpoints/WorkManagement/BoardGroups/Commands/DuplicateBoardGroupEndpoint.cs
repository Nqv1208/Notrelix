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
            .WithSummary("Duplicate a group and its items");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DuplicateBoardGroupCommand(groupId), cancellationToken);
        return result.ToCreatedResult();
    }
}
