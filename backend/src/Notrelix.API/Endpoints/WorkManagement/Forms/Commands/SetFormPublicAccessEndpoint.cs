using Notrelix.API.Contracts.WorkManagement.Forms.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SetFormPublicAccess;

namespace Notrelix.API.Endpoints.WorkManagement.Forms.Commands;

public static class SetFormPublicAccessEndpoint
{
    public static IEndpointRouteBuilder MapSetFormPublicAccess(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/public-access", HandleAsync)
            .WithName("WorkManagement.Forms.SetPublicAccess")
            .WithTags("WorkManagement.Forms")
            .WithSummary("Switch a form public or private (workspace Owner or Admin)");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid formId,
        SetFormPublicAccessRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetFormPublicAccessCommand(formId, body.Enabled), cancellationToken);
        return result.ToApiResult();
    }
}
