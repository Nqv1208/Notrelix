using MediatR;
using Notrelix.API.Endpoints.Attachments;
using Notrelix.API.Endpoints.Activity;
using Notrelix.API.Endpoints.Auth;
using Notrelix.API.Endpoints.Boards;
using Notrelix.API.Endpoints.Cards;
using Notrelix.API.Endpoints.Checklists;
using Notrelix.API.Endpoints.Comments;
using Notrelix.API.Endpoints.Document;
using Notrelix.API.Endpoints.Extensibility;
using Notrelix.API.Endpoints.Health;
using Notrelix.API.Endpoints.Labels;
using Notrelix.API.Endpoints.Lists;
using Notrelix.API.Endpoints.Users;
using Notrelix.API.Endpoints.Workspaces;
using Notrelix.API.Endpoints.Notifications;
using Notrelix.Application.Common.Models;

namespace Notrelix.API.Extensions;

/// <summary>
/// Central endpoint registration and Result → IResult conversion helpers.
/// </summary>
public static class EndpointExtensions
{
    // ── Central registration ──────────────────────────────────────

    /// <summary>
    /// Maps all Minimal API endpoint groups.
    /// Called once from Program.cs.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        // Auth & Identity
        app.MapAuthEndpoints();
        app.MapUsersEndpoints();
        app.MapHealthEndpoints();

        // Workspace
        app.MapWorkspaceEndpoints();

        // Document
        app.MapDocumentEndpoints();

        // Boards
        app.MapBoardEndpoints();
        app.MapBoardMemberEndpoints();
        app.MapBoardViewEndpoints();

        // Lists
        app.MapListEndpoints();

        // Cards
        app.MapCardEndpoints();
        app.MapCardMemberEndpoints();
        app.MapCardLabelEndpoints();

        // Labels
        app.MapLabelEndpoints();

        // Checklists
        app.MapChecklistEndpoints();

        // Comments
        app.MapCommentEndpoints();

        // Attachments
        app.MapAttachmentEndpoints();

        // Activity
        app.MapActivityEndpoints();

        // Notifications
        app.MapNotificationEndpoints();

        // Extensibility / Automations
        app.MapAutomationEndpoints();

        return app;
    }

    // ── Result → IResult helpers ──────────────────────────────────

    /// <summary>
    /// Convert Result (no data) → 200 OK or 400 BadRequest.
    /// </summary>
    public static IResult ToApiResult(this Result result)
    {
        return result.Succeeded
            ? Results.Ok()
            : Results.BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Convert Result&lt;T&gt; → 200 OK with data or 400 BadRequest.
    /// </summary>
    public static IResult ToApiResult<T>(this Result<T> result)
    {
        return result.Succeeded
            ? Results.Ok(result.Data)
            : Results.BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Convert Result&lt;T&gt; → 201 Created with data or 400 BadRequest.
    /// </summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, string? location = null)
    {
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return location is not null
            ? Results.Created(location, result.Data)
            : Results.Created($"/{result.Data}", result.Data);
    }

    /// <summary>
    /// Convert Result → 204 NoContent or 400 BadRequest.
    /// </summary>
    public static IResult ToNoContentResult(this Result result)
    {
        return result.Succeeded
            ? Results.NoContent()
            : Results.BadRequest(new { errors = result.Errors });
    }
}
