using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.ResourcePermissions.Queries.GetResourcePermissions;

namespace Notrelix.API.Endpoints.Governance.ResourcePermissions.Queries;

public static class GetResourcePermissionsEndpoint
{
    public static IEndpointRouteBuilder MapGetResourcePermissions(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Governance.ResourcePermissions.Get")
            .WithTags("Governance.ResourcePermissions")
            .WithSummary("Get permissions for a resource");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetResourcePermissionsQuery(resourceType, resourceId), cancellationToken);
        return result.ToApiResult();
    }
}
