using Notrelix.API.Contracts.Governance.ResourcePermissions.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.DTOs;
using Notrelix.Application.Features.Governance.ResourcePermissions.Commands.GrantResourcePermission;

namespace Notrelix.API.Endpoints.Governance.ResourcePermissions.Commands;

public static class GrantResourcePermissionEndpoint
{
    public static IEndpointRouteBuilder MapGrantResourcePermission(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("Governance.ResourcePermissions.Grant")
            .WithTags("Governance.ResourcePermissions")
            .WithSummary("Grant a permission to a resource")
            .WithDescription("Grants a level to a subject. A subject that already holds the level is a no-op; changing the level of an existing grant must name its version (ExpectedVersion), and a stale or missing version answers 412.")
            .Produces<ResourcePermissionDto>(StatusCodes.Status201Created)
            .ProducesProblems(StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        string resourceType,
        Guid resourceId,
        GrantPermissionRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GrantResourcePermissionCommand(resourceType, resourceId, body.SubjectType, body.SubjectId, body.Level, body.ExpiresAt, body.ExpectedVersion),
            cancellationToken);
        return result.ToCreatedResult();
    }
}
