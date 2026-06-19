using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Workspaces.Commands;

public static class CreateWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapCreateWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("Workspaces.Workspaces.CreateWorkspace")
            .WithTags("Workspaces.Workspaces")
            .WithSummary("Create a new workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateWorkspaceCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult();
    }
}
