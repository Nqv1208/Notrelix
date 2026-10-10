using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.UnpinResource;

namespace Notrelix.API.Endpoints.Workspaces.Navigation.Commands;

public static class UnpinResourceEndpoint
{
    public static IEndpointRouteBuilder MapUnpinResource(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/{kind}/{resourceId:guid}", HandleAsync)
            .WithName("Workspaces.Navigation.Unpin")
            .WithTags("Workspaces.Navigation")
            .WithSummary("Remove the current member's pin")
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Idempotent: unpinning a resource that is not pinned succeeds without change.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string kind,
        Guid resourceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnpinResourceCommand(workspaceId, kind, resourceId), cancellationToken);
        return result.ToNoContentResult();
    }
}
