using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.DisableShareLink;

namespace Notrelix.API.Endpoints.Governance.ShareLinks.Commands;

public static class DisableShareLinkEndpoint
{
    public static IEndpointRouteBuilder MapDisableShareLink(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{shareLinkId:guid}", HandleAsync)
            .WithName("Governance.ShareLinks.Disable")
            .WithTags("Governance.ShareLinks")
            .WithSummary("Disable a share link")
            .WithDescription("Authorized on the shared resource named by the route with the action that creating a link there needs, so a Board manager may turn off a link on their board. A link that does not belong to that resource answers NotFound; a link already off answers 204 and changes nothing.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        Guid shareLinkId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DisableShareLinkCommand(resourceType, resourceId, shareLinkId),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
