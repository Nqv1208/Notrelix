using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Members.Commands.SetDefaultWorkspace;

namespace Notrelix.API.Endpoints.Workspaces.Members.Commands;

public sealed record SetDefaultWorkspaceRequest(Guid WorkspaceId);

public static class SetDefaultWorkspaceEndpoint
{
    public static IEndpointRouteBuilder MapSetDefaultWorkspace(this IEndpointRouteBuilder group)
    {
        group.MapAuthenticatedPut("/default-workspace", HandleAsync)
            .WithName("Workspaces.Members.SetDefaultWorkspace")
            .WithTags("Workspaces.Members")
            .WithSummary("Choose the workspace that opens with the app")
            .WithDescription("The caller must be an active member of the workspace; a workspace the caller cannot open answers 404, and an archived workspace is refused with 400 (a business-rule failure). Choosing the current default succeeds and changes nothing.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(SetDefaultWorkspaceRequest body, ISender sender)
    {
        var result = await sender.Send(new SetDefaultWorkspaceCommand(body.WorkspaceId));
        return result.ToNoContentResult();
    }
}
