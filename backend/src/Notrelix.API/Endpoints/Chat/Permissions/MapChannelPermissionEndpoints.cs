using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Permissions.Commands.ClearChannelPermissions;
using Notrelix.Application.Features.Chat.Permissions.Commands.SetChannelPermissions;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Chat.Permissions.Queries.GetChannelPermissions;

namespace Notrelix.API.Endpoints.Chat.Permissions;

/// <summary>Per-channel permission overrides by role: read, replace and clear (UC-CHT-H01).</summary>
public static class MapChannelPermissionEndpoints
{
    public static IEndpointRouteBuilder MapChannelPermissionRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/channels/{channelId:guid}/permissions")
            .WithTags("Chat.Permissions")
            .WithOpenApi();

        group.MapWorkspaceGet("/", GetAsync)
            .WithName("Chat.Permissions.Get")
            .WithSummary("Read a channel's permission overrides")
            .WithDescription("Anyone who can see the channel reads the overrides and the version to send back. A channel the caller cannot see is 404.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/", SetAsync)
            .WithName("Chat.Permissions.Set")
            .WithSummary("Replace a channel's permission overrides")
            .WithDescription("The set is the whole truth: sending fewer entries removes the rest. Needs a channel manager, a Workspace Owner or Admin. A deny beats an allow given for the same role and action. An allow above what the role (or the caller) holds is 422 permission-exceeds-role; denying a manager invites is 422 permission-lockout. A stale expectedVersion is 412; an archived channel is 409 channel-archived. A change refreshes the channel's clients (chat.space.changed).")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed, StatusCodes.Status422UnprocessableEntity);

        group.MapWorkspaceDelete("/", ClearAsync)
            .WithName("Chat.Permissions.Delete")
            .WithSummary("Remove every permission override of a channel")
            .WithDescription("Every role is back on its default. Pass expectedVersion as a query parameter; 412 when it is stale. A channel with no overrides changes nothing. Returns the permissions with the new version.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status412PreconditionFailed);

        return app;
    }

    private static async Task<IResult> GetAsync(Guid workspaceId, Guid channelId, ISender sender)
    {
        var result = await sender.Send(new GetChannelPermissionsQuery(workspaceId, channelId));
        return result.ToApiResult();
    }

    private static async Task<IResult> SetAsync(Guid workspaceId, Guid channelId, SetChannelPermissionsRequest body, ISender sender)
    {
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");
        if (body.Entries is null)
            return EndpointExtensions.InvalidInput("entries is required.");

        var entries = body.Entries
            .Select(e => new ChannelPermissionEntryDto(e.Subject ?? string.Empty, e.Action ?? string.Empty, e.Effect ?? string.Empty))
            .ToList();
        var result = await sender.Send(new SetChannelPermissionsCommand(workspaceId, channelId, entries, expectedVersion));
        return result.ToApiResult();
    }

    private static async Task<IResult> ClearAsync(Guid workspaceId, Guid channelId, [FromQuery] long? expectedVersion, ISender sender)
    {
        if (expectedVersion is not { } version)
            return EndpointExtensions.InvalidInput("expectedVersion is required as a query parameter.");

        var result = await sender.Send(new ClearChannelPermissionsCommand(workspaceId, channelId, version));
        return result.ToApiResult();
    }
}
