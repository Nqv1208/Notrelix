using Notrelix.API.Contracts.Workspaces.Workspaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class TransferOwnershipEndpoint
{
    public static IEndpointRouteBuilder MapTransferOwnership(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/transfer-ownership", HandleAsync)
            .WithName("Workspaces.Workspaces.TransferOwnership")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Transfer workspace ownership to another member")
            .WithDescription("Needs a single-use step-up proof for the TransferWorkspaceOwnership purpose, bound to the caller, the session and the purpose. Without one it answers 412 identity.security.step-up-required; a stale, spent or foreign proof answers 400 identity.security.step-up-invalid. Ownership never changes on a refusal.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        TransferOwnershipRequest request,
        ISender sender)
    {
        var result = await sender.Send(new TransferOwnershipCommand(workspaceId, request.NewOwnerId, request.ExpectedVersion, request.StepUpToken));
        return result.ToNoContentResult();
    }
}
