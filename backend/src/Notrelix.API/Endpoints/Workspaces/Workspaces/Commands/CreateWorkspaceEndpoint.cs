using Notrelix.API.Contracts.Workspaces.Workspaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Context;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class CreateWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapCreateWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPost("/", HandleAsync)
            .WithName("Workspaces.Workspaces.CreateWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Create a new workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid accountId,
        CreateWorkspaceRequest body,
        ICurrentTenantContext tenant,
        ICurrentUser currentUser,
        ISender sender)
    {
        tenant.SetAccount(accountId, currentUser.UserId);
        var command = new CreateWorkspaceCommand(body.Name, body.Description, body.IsPersonal);
        var result = await sender.Send(command);
        return result.ToCreatedResult();
    }
}
