using Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ArchiveBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class ArchiveBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/archive", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardGroups.Archive")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Archive a group");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid groupId,
        BoardGroupVersionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardGroupCommand(groupId, body.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
