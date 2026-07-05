using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnassignBoardItemMember;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class UnassignBoardItemMemberEndpoint
{
    public static IEndpointRouteBuilder MapUnassignBoardItemMember(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/assignees/{userId:guid}", HandleAsync)
            .WithName("WorkManagement.BoardItems.UnassignMember")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Unassign a member from board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        Guid userId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnassignBoardItemMemberCommand(itemId, userId), cancellationToken);
        return result.ToNoContentResult();
    }
}
