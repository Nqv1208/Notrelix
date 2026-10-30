using Notrelix.API.Contracts.Workspaces.Spaces.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;

namespace Notrelix.API.Endpoints.Workspaces.Spaces.Commands;

public static class CreateSpaceEndpoint
{
    public static IEndpointRouteBuilder MapCreateSpace(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Workspaces.Spaces.CreateSpace")
            .WithTags("Workspaces.Spaces")
            .WithIdempotencyKey()
            .WithSummary("Create a new space in the workspace")
            .Produces<Guid>(StatusCodes.Status201Created, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateSpaceRequest request,
        ISender sender)
    {
        var result = await sender.Send(new CreateSpaceCommand(
            workspaceId,
            request.Name,
            request.Visibility,
            request.Description,
            request.SpaceType,
            request.ParentSpaceId,
            request.PreviousSiblingId,
            request.NextSiblingId));
        return result.ToCreatedResult();
    }
}
