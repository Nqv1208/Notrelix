using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Channels.Commands.ArchiveChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.CreateChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.DeleteChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.ReorderChannels;
using Notrelix.Application.Features.Chat.Channels.Commands.UpdateChannel;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;

namespace Notrelix.API.Endpoints.Chat.Channels;

/// <summary>Channels of a Workspace: create, browse, read, rename, archive, delete and order (UC-CHT-A01 to A08).</summary>
public static class MapChannelEndpoints
{
    public static IEndpointRouteBuilder MapChannelRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/channels")
            .WithTags("Chat.Channels")
            .WithOpenApi();

        group.MapWorkspaceGet("/", ListAsync)
            .WithName("Chat.Channels.List")
            .WithSummary("List the channels the caller can see")
            .WithDescription("By default the caller's own channels, in sidebar order. With browse=true, the public channels the caller is not in; a Guest browses nothing.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden);

        group.MapWorkspacePost("/", CreateAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Channels.Create")
            .WithSummary("Create a public or private channel")
            .WithDescription("The caller becomes its first manager. A name already used by a live channel is 409 name-conflict.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status409Conflict);

        group.MapWorkspaceGet("/{channelId:guid}", GetAsync)
            .WithName("Chat.Channels.Get")
            .WithSummary("Get a channel with its members")
            .WithDescription("A private channel the caller is not in, and a channel that does not exist, are both 404.")
            .ProducesProblems(StatusCodes.Status404NotFound);

        group.MapWorkspacePatch("/order", ReorderAsync)
            .WithName("Chat.Channels.Reorder")
            .WithSummary("Set the sidebar order of channels")
            .WithDescription("Each channel moved needs the manage right on it. A stale expectedVersion on any channel refuses the batch with 412 and changes nothing.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePatch("/{channelId:guid}", UpdateAsync)
            .WithName("Chat.Channels.Update")
            .WithSummary("Rename a channel, set its topic or its slow mode")
            .WithDescription("Needs a channel manager, a Workspace Owner or Admin. slowModeSeconds (0 to 3600) is the least time between two messages of one person; 0 turns it off and a request that leaves it out keeps it; a change is announced to the channel as chat.space.changed with the change slow-mode. A stale expectedVersion is 412; an archived channel is 409 channel-archived.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspacePost("/{channelId:guid}/archive", ArchiveAsync)
            .WithName("Chat.Channels.Archive")
            .WithSummary("Archive or unarchive a channel")
            .WithDescription("An archived channel is read-only. Unarchiving checks the name again: a name taken meanwhile is 409 name-conflict.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        group.MapWorkspaceDelete("/{channelId:guid}", DeleteAsync)
            .WithName("Chat.Channels.Delete")
            .WithSummary("Delete a channel")
            .WithDescription("Soft-deletes the channel and releases its name. Pass expectedVersion as a query parameter; 412 when it is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, bool? browse, ISender sender)
    {

        var result = await sender.Send(new ListChannelsQuery(workspaceId, browse ?? false));
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateAsync(Guid workspaceId, CreateChannelRequest body, ISender sender)
    {

        var result = await sender.Send(new CreateChannelCommand(workspaceId, body.Name, body.Topic, body.IsPrivate, body.GroupId));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetAsync(Guid workspaceId, Guid channelId, ISender sender)
    {

        var result = await sender.Send(new GetChannelQuery(workspaceId, channelId));
        return result.ToApiResult();
    }

    private static async Task<IResult> ReorderAsync(Guid workspaceId, ReorderChannelsRequest body, ISender sender)
    {
        if (body.Placements is null)
            return EndpointExtensions.InvalidInput("placements is required.");
        if (body.Placements.Any(p => p.ExpectedVersion is null))
            return EndpointExtensions.InvalidInput("expectedVersion is required for every channel.");

        var placements = body.Placements
            .Select(p => new ChannelPlacement(p.ChannelId, p.GroupId, p.Position, p.ExpectedVersion!.Value))
            .ToList();
        var result = await sender.Send(new ReorderChannelsCommand(workspaceId, placements));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateAsync(Guid workspaceId, Guid channelId, UpdateChannelRequest body, ISender sender)
    {
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");

        var result = await sender.Send(new UpdateChannelCommand(workspaceId, channelId, body.Name, body.Topic, expectedVersion, body.SlowModeSeconds));
        return result.ToApiResult();
    }

    private static async Task<IResult> ArchiveAsync(Guid workspaceId, Guid channelId, ArchiveChannelRequest body, ISender sender)
    {
        if (body.Archived is not { } archived)
            return EndpointExtensions.InvalidInput("archived is required.");
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");

        var result = await sender.Send(new ArchiveChannelCommand(workspaceId, channelId, archived, expectedVersion));
        return result.ToApiResult();
    }

    private static async Task<IResult> DeleteAsync(Guid workspaceId, Guid channelId, [FromQuery] long? expectedVersion, ISender sender)
    {
        if (expectedVersion is not { } version)
            return EndpointExtensions.InvalidInput("expectedVersion is required as a query parameter.");

        var result = await sender.Send(new DeleteChannelCommand(workspaceId, channelId, version));
        return result.ToNoContentResult();
    }
}
