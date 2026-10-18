using Notrelix.API.Contracts.Workspaces.Navigation.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;

namespace Notrelix.API.Endpoints.Workspaces.Navigation.Commands;

public static class RecordRecentResourceEndpoint
{
    public static IEndpointRouteBuilder MapRecordRecentResource(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Workspaces.Navigation.RecordRecent")
            .WithTags("Workspaces.Navigation")
            .WithSummary("Record that the current member opened a Board or Doc")
            .Produces(StatusCodes.Status204NoContent)
            .WithDescription("Upserts the member's recent entry and keeps the 30 most recent per workspace.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        RecordRecentResourceRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RecordRecentResourceCommand(workspaceId, body.Kind, body.ResourceId),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
