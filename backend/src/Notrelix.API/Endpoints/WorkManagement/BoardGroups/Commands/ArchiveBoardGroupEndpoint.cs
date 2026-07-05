using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class ArchiveBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.BoardGroups.Archive")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Archive a group");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardGroupCommand(groupId), cancellationToken);
        return result.ToNoContentResult();
    }
}
