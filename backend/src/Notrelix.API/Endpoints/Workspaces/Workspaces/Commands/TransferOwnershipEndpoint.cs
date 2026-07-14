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
            .WithSummary("Transfer workspace ownership to another member");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        TransferOwnershipRequest request,
        ISender sender)
    {
        var result = await sender.Send(new TransferOwnershipCommand(workspaceId, request.NewOwnerId, request.ExpectedVersion));
        return result.ToNoContentResult();
    }
}
