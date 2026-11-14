using Notrelix.API.Contracts.Chat.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Chat.Conversations.Commands.AddConversationParticipants;
using Notrelix.Application.Features.Chat.Conversations.Commands.LeaveConversation;
using Notrelix.Application.Features.Chat.Conversations.Commands.SetConversationVisibility;
using Notrelix.Application.Features.Chat.Conversations.Commands.StartConversation;
using Notrelix.Application.Features.Chat.Conversations.Queries.ListConversations;

namespace Notrelix.API.Endpoints.Chat.Conversations;

/// <summary>Direct and group conversations: start, list, add people, leave, hide or show (UC-CHT-E01 to E05, E07).</summary>
public static class MapConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationRoutes(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/workspaces/{workspaceId:guid}/chat/conversations")
            .WithTags("Chat.Conversations")
            .WithOpenApi();

        group.MapWorkspaceGet("/", ListAsync)
            .WithName("Chat.Conversations.List")
            .WithSummary("List the caller's direct and group conversations")
            .WithDescription("Newest activity first, each with its participants, the newest message as a preview and the caller's unread count. Conversations the caller hid are left out. limit is 1 to 100, default 50.")
            .Produces<IReadOnlyList<Application.Features.Chat.Conversations.DTOs.ConversationDto>>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden);

        group.MapWorkspacePost("/", StartAsync)
            .WithIdempotencyKey()
            .WithName("Chat.Conversations.Start")
            .WithSummary("Start a direct or group conversation")
            .WithDescription("A direct conversation is one per pair of people: starting it again returns the conversation that exists (200), otherwise it is created (201). A group has at least three people counted with the caller and at most eight: over the cap is 422 limit-reached. Starting with yourself is 400. A person who is not an active member of the Workspace, a person blocked in either direction, or (for a Guest) a person with no shared space is 404. A group lists the people who could not be added under skipped.")
            .Produces<Application.Features.Chat.Conversations.DTOs.StartConversationResultDto>(StatusCodes.Status200OK)
            .Produces<Application.Features.Chat.Conversations.DTOs.StartConversationResultDto>(StatusCodes.Status201Created)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        group.MapWorkspacePost("/{conversationId:guid}/participants", AddParticipantsAsync)
            .WithName("Chat.Conversations.AddParticipants")
            .WithSummary("Add people to a group conversation")
            .WithDescription("Any participant may add people to a group. A direct conversation takes no one else (400 kind-not-supported). The group stays within eight people: over the cap is 422 limit-reached. A person outside the Workspace is listed under skipped. A conversation the caller is not in is 404.")
            .Produces<Application.Features.Chat.Conversations.DTOs.AddConversationParticipantsResultDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status422UnprocessableEntity);

        group.MapWorkspaceDelete("/{conversationId:guid}/participants/me", LeaveAsync)
            .WithName("Chat.Conversations.Leave")
            .WithSummary("Leave a group conversation")
            .WithDescription("The remaining participants see a system message. A direct conversation cannot be left (400 kind-not-supported). When the last participant leaves, the conversation closes. A conversation the caller is not in is 404.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        group.MapWorkspacePut("/{conversationId:guid}/visibility", SetVisibilityAsync)
            .WithName("Chat.Conversations.SetVisibility")
            .WithSummary("Hide or show a conversation in the caller's list")
            .WithDescription("Only the caller's own list changes; the other participants keep the conversation. A new message in it shows it again. Idempotent. A conversation the caller is not in is 404.")
            .Produces<Application.Features.Chat.Conversations.DTOs.ConversationVisibilityDto>(StatusCodes.Status200OK)
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(Guid workspaceId, int? limit, ISender sender)
    {
        var result = await sender.Send(new ListConversationsQuery(workspaceId, limit ?? 50));
        return result.ToApiResult();
    }

    private static async Task<IResult> StartAsync(Guid workspaceId, StartConversationRequest body, ISender sender)
    {
        if (body.Kind is null)
            return EndpointExtensions.InvalidInput("kind is required: direct or group.");

        var result = await sender.Send(new StartConversationCommand(workspaceId, body.Kind, body.UserId, body.UserIds, body.Name));
        if (!result.Succeeded)
            return result.ToApiResult();

        return result.Data!.Created
            ? Results.Json(result.Data, statusCode: StatusCodes.Status201Created)
            : Results.Ok(result.Data);
    }

    private static async Task<IResult> AddParticipantsAsync(Guid workspaceId, Guid conversationId, AddConversationParticipantsRequest body, ISender sender)
    {
        if (body.UserIds is null)
            return EndpointExtensions.InvalidInput("userIds is required.");

        var result = await sender.Send(new AddConversationParticipantsCommand(workspaceId, conversationId, body.UserIds));
        return result.ToApiResult();
    }

    private static async Task<IResult> LeaveAsync(Guid workspaceId, Guid conversationId, ISender sender)
    {
        var result = await sender.Send(new LeaveConversationCommand(workspaceId, conversationId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> SetVisibilityAsync(Guid workspaceId, Guid conversationId, SetConversationVisibilityRequest body, ISender sender)
    {
        if (body.Hidden is not { } hidden)
            return EndpointExtensions.InvalidInput("hidden is required.");

        var result = await sender.Send(new SetConversationVisibilityCommand(workspaceId, conversationId, hidden));
        return result.ToApiResult();
    }
}
