using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DeleteBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class DeleteBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapDeleteBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardGroups.Delete")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Delete a board group");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteBoardGroupCommand(groupId), cancellationToken);
        return result.ToNoContentResult();
    }
}
