using MediatR;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Interfaces;
using Notrelix.Application.Features.Workspaces.Commands;
using Notrelix.Application.Features.Workspaces.Queries;

namespace Notrelix.API.Endpoints.Workspaces;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/workspaces")
            .WithTags("Workspaces")
            .RequireAuthorization()
            .WithOpenApi();

        // ── Workspace CRUD ───────────────────────────────────────
        group.MapPost("/", CreateWorkspace)
            .WithName("CreateWorkspace")
            .WithSummary("Create a new workspace");

        group.MapGet("/", GetUserWorkspaces)
            .WithName("GetUserWorkspaces")
            .WithSummary("Get current user's workspaces");

        group.MapGet("/{slug}", GetWorkspace)
            .WithName("GetWorkspace")
            .WithSummary("Get workspace by slug");

        group.MapPatch("/{slug}", UpdateWorkspace)
            .WithName("UpdateWorkspace")
            .WithSummary("Update workspace settings");

        group.MapDelete("/{slug}", ArchiveWorkspace)
            .WithName("ArchiveWorkspace")
            .WithSummary("Archive (soft delete) a workspace");

        // ── Members ──────────────────────────────────────────────
        group.MapGet("/{slug}/members", GetMembers)
            .WithName("GetWorkspaceMembers")
            .WithSummary("Get workspace members");

        group.MapPatch("/{slug}/members/{userId:guid}", UpdateMemberRole)
            .WithName("UpdateMemberRole")
            .WithSummary("Update a member's role");

        group.MapDelete("/{slug}/members/{userId:guid}", RemoveMember)
            .WithName("RemoveMember")
            .WithSummary("Remove a member from workspace");

        // ── Invitations ──────────────────────────────────────────
        group.MapPost("/{slug}/invitations", InviteMember)
            .WithName("InviteMember")
            .WithSummary("Invite a member to workspace");

        // ── Activity ─────────────────────────────────────────────
        group.MapGet("/{slug}/activity", GetActivity)
            .WithName("GetWorkspaceActivity")
            .WithSummary("Get workspace activity log");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────

    private static async Task<IResult> CreateWorkspace(
        CreateWorkspaceCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetUserWorkspaces(
        ISender sender,
        ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            return Results.Unauthorized();

        var result = await sender.Send(new GetUserWorkspacesQuery(currentUser.UserId));
        return result.ToApiResult();
    }

    private static async Task<IResult> GetWorkspace(
        string slug,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceBySlugQuery(slug));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateWorkspace(
        string slug,
        UpdateWorkspaceCommand command,
        ISender sender)
    {
        // Slug is used to resolve WorkspaceId in the handler
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> ArchiveWorkspace(
        string slug,
        ISender sender)
    {
        var result = await sender.Send(new ArchiveWorkspaceBySlugCommand(slug));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> GetMembers(
        string slug,
        ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceMembersBySlugQuery(slug));
        return result.ToApiResult();
    }

    private static async Task<IResult> UpdateMemberRole(
        string slug,
        Guid userId,
        UpdateMemberRoleRequest body,
        ISender sender)
    {
        var result = await sender.Send(new UpdateMemberRoleBySlugCommand(slug, userId, body.Role));
        return result.ToApiResult();
    }

    private static async Task<IResult> RemoveMember(
        string slug,
        Guid userId,
        ISender sender)
    {
        var result = await sender.Send(new RemoveMemberBySlugCommand(slug, userId));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> InviteMember(
        string slug,
        InviteMemberRequest body,
        ISender sender)
    {
        var result = await sender.Send(new InviteMemberBySlugCommand(slug, body.Email, body.Role));
        return result.ToCreatedResult();
    }

    private static async Task<IResult> GetActivity(
        string slug,
        ISender sender,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetWorkspaceActivityBySlugQuery(slug, page, pageSize));
        return result.ToApiResult();
    }
}

// ── Request DTOs for route-bound endpoints ─────────────────────
public record UpdateMemberRoleRequest(string Role);
public record InviteMemberRequest(string Email, string Role);
