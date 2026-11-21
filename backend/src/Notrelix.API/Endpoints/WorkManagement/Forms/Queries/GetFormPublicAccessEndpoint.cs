using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetFormPublicAccess;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Queries;

public static class GetFormPublicAccessEndpoint
{
    public static IEndpointRouteBuilder MapGetFormPublicAccess(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/public-access", HandleAsync)
            .WithName("WorkManagement.Forms.GetPublicAccess")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Whether a form is public, and its public key (workspace Owner or Admin)");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid formId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFormPublicAccessQuery(formId), cancellationToken);
        return result.ToApiResult();
    }
}
