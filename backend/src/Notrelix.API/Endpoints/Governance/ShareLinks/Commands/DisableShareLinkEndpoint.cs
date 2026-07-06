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
            .WithSummary("Disable a share link");
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
            new DisableShareLinkCommand(shareLinkId),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
