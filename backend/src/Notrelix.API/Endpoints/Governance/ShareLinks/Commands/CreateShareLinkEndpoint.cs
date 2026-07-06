using Notrelix.API.Contracts.Governance.ShareLinks.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.ShareLinks.Commands.CreateShareLink;

namespace Notrelix.API.Endpoints.Governance.ShareLinks.Commands;

public static class CreateShareLinkEndpoint
{
    public static IEndpointRouteBuilder MapCreateShareLink(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("Governance.ShareLinks.Create")
            .WithTags("Governance.ShareLinks")
            .WithSummary("Create a share link for a resource");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        CreateShareLinkRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateShareLinkCommand(Enum.Parse<ResourceType>(resourceType, ignoreCase: true), resourceId, body.Level, body.ExpiresAt),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
