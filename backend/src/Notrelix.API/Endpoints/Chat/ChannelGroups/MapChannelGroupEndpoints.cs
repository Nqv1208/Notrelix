using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.CreateChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.DeleteChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Commands.UpdateChannelGroup;
using Notrelix.Application.Features.Chat.ChannelGroups.Queries.ListChannelGroups;

namespace Notrelix.API.Endpoints.Chat.ChannelGroups;

/// <summary>Sidebar categories of a Workspace (UC-CHT-A02). Deleting one leaves its channels in place.</summary>
public static class MapChannelGroupEndpoints
{
    public static IEndpointRouteBuilder MapChannelGroupRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/channel-groups")
            .WithTags("Chat.ChannelGroups")
            .WithOpenApi();

        group.MapWorkspaceGet("/", ListAsync)
            .WithName("Chat.ChannelGroups.List")
            .WithSummary("List the sidebar categories with their names")
            .WithDescription("In sidebar order. Every Workspace member reads all categories; a Guest reads only the categories that hold a channel they are in.")
            .ProducesProblems(StatusCodes.Status403Forbidden);

        group.MapWorkspacePost("/", CreateAsync)
            .WithIdempotencyKey()
            .WithName("Chat.ChannelGroups.Create")
            .WithSummary("Create a sidebar category")
            .WithDescription("Names are unique in the Workspace, ignoring case: 409 name-conflict otherwise.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status409Conflict);

        group.MapWorkspacePatch("/{groupId:guid}", UpdateAsync)
            .WithName("Chat.ChannelGroups.Update")
            .WithSummary("Rename a category or move it")
            .WithDescription("A stale expectedVersion is 412.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspaceDelete("/{groupId:guid}", DeleteAsync)
            .WithName("Chat.ChannelGroups.Delete")
            .WithSummary("Delete a category")
            .WithDescription("Its channels are not deleted; they leave the category and keep their place. Pass expectedVersion as a query parameter; 412 when it is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new ListChannelGroupsQuery(workspaceId));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateAsync(Guid workspaceId, CreateChannelGroupRequest body, ISender sender)
    {

        var result = await sender.Send(new CreateChannelGroupCommand(workspaceId, body.Name, body.Position));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> UpdateAsync(Guid workspaceId, Guid groupId, UpdateChannelGroupRequest body, ISender sender)
    {
        if (body.Position is not { } position)
            return EndpointExtensions.InvalidInput("position is required.");
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");

        var result = await sender.Send(new UpdateChannelGroupCommand(workspaceId, groupId, body.Name, position, expectedVersion));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(Guid workspaceId, Guid groupId, [FromQuery] long? expectedVersion, ISender sender)
    {
        if (expectedVersion is not { } version)
            return EndpointExtensions.InvalidInput("expectedVersion is required as a query parameter.");

        var result = await sender.Send(new DeleteChannelGroupCommand(workspaceId, groupId, version));
        return result.ToNoContentResult();
    }
}
