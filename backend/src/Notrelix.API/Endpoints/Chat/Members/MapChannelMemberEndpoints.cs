using Microsoft.AspNetCore.Mvc;
using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Members.Commands.AddChannelMembers;
using Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;
using Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember;

namespace Notrelix.API.Endpoints.Chat.Members;

/// <summary>Who is in a channel: join a public one, add people, leave or remove (UC-CHT-A05 to A07).</summary>
public static class MapChannelMemberEndpoints
{
    public static IEndpointRouteBuilder MapChannelMemberRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/channels/{channelId:guid}/members")
            .WithTags("Chat.Members")
            .WithOpenApi();

        group.MapWorkspacePost("/", AddAsync)
            .WithName("Chat.Members.Add")
            .WithSummary("Add Workspace members to a channel")
            .WithDescription("In a private channel any member may invite; in a public channel only a manager, a Workspace Owner or Admin may add people. A person who is not in the Workspace is listed under skipped.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePost("/me", JoinAsync)
            .WithName("Chat.Members.Join")
            .WithSummary("Join a public channel")
            .WithDescription("A private channel is 404 to a person who is not in it; a Guest is 403. Joining again changes nothing.")
            .ProducesProblems(StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspaceDelete("/{userId:guid}", RemoveAsync)
            .WithName("Chat.Members.Remove")
            .WithSummary("Leave a channel, or remove a member from it")
            .WithDescription("A person may remove themselves; removing someone else needs a manager, a Workspace Owner or Admin. The last manager of a live channel cannot leave: 422 last-manager.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> AddAsync(Guid workspaceId, Guid channelId, AddChannelMembersRequest body, ISender sender)
    {
        if (body.UserIds is null)
            return EndpointExtensions.InvalidInput("userIds is required.");

        var result = await sender.Send(new AddChannelMembersCommand(workspaceId, channelId, body.UserIds));
        return result.ToApiResult();
    }

    private static async Task<IResult> JoinAsync(Guid workspaceId, Guid channelId, ISender sender)
    {

        var result = await sender.Send(new JoinChannelCommand(workspaceId, channelId));
        return result.ToApiResult();
    }

    private static async Task<IResult> RemoveAsync(Guid workspaceId, Guid channelId, Guid userId, ISender sender)
    {

        var result = await sender.Send(new RemoveChannelMemberCommand(workspaceId, channelId, userId));
        return result.ToNoContentResult();
    }
}
