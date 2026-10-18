using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;

namespace Notrelix.API.Endpoints.Workspaces.Navigation.Commands;

public static class PinResourceEndpoint
{
    public static IEndpointRouteBuilder MapPinResource(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/{kind}/{resourceId:guid}", HandleAsync)
            .WithName("Workspaces.Navigation.Pin")
            .WithTags("Workspaces.Navigation")
            .WithSummary("Pin a Board or Doc for the current member")
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Idempotent: pinning an already pinned resource is a no-op. At most 50 pins per member per workspace.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string kind,
        Guid resourceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PinResourceCommand(workspaceId, kind, resourceId), cancellationToken);
        return result.ToNoContentResult();
    }
}
