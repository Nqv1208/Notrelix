using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UnarchiveBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class UnarchiveBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapUnarchiveBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapPost("/unarchive", HandleAsync)
            .WithName("WorkManagement.BoardGroups.Unarchive")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Unarchive a group");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnarchiveBoardGroupCommand(groupId), cancellationToken);
        return result.ToNoContentResult();
    }
}
