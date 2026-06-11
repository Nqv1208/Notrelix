using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Notrelix.API.Extensions;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Features.Governance.Commands;
using Notrelix.Application.Features.Governance.Queries;

namespace Notrelix.API.Endpoints.Governance;

public static class GovernanceEndpoints
{
    public static IEndpointRouteBuilder MapGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .RequireAuthorization()
            .WithTags("Governance")
            .WithOpenApi();

        // GET /api/v1/resources/{resourceType}/{resourceId}/permissions
        group.MapGet("/resources/{resourceType}/{resourceId:guid}/permissions", GetResourcePermissions)
            .WithName("GetResourcePermissions")
            .WithSummary("Get active permissions for a specific resource");

        // POST /api/v1/resources/{resourceType}/{resourceId}/permissions
        group.MapPost("/resources/{resourceType}/{resourceId:guid}/permissions", GrantResourcePermission)
            .WithName("GrantResourcePermission")
            .WithSummary("Grant permission to a user or subject for a specific resource");

        // DELETE /api/v1/resources/{resourceType}/{resourceId}/permissions/{permissionId}
        group.MapDelete("/resources/{resourceType}/{resourceId:guid}/permissions/{permissionId:guid}", RevokeResourcePermission)
            .WithName("RevokeResourcePermission")
            .WithSummary("Revoke a permission from a resource");

        // POST /api/v1/resources/{resourceType}/{resourceId}/share-links
        group.MapPost("/resources/{resourceType}/{resourceId:guid}/share-links", CreateShareLink)
            .WithName("CreateShareLink")
            .WithSummary("Create a public share link for a resource");

        // DELETE /api/v1/share-links/{shareLinkId}
        group.MapDelete("/share-links/{shareLinkId:guid}", DisableShareLink)
            .WithName("DisableShareLink")
            .WithSummary("Disable an active share link");

        return app;
    }

    private static async Task<IResult> GetResourcePermissions(
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        string resourceType,
        Guid resourceId,
        ISender sender)
    {
        var query = new GetResourcePermissionsQuery(workspaceId, resourceType, resourceId);
        var result = await sender.Send(query);
        return result.ToApiResult();
    }

    private static async Task<IResult> GrantResourcePermission(
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        string resourceType,
        Guid resourceId,
        GrantPermissionRequest request,
        ISender sender)
    {
        var command = new GrantResourcePermissionCommand(
            workspaceId,
            resourceType,
            resourceId,
            request.SubjectType,
            request.SubjectId,
            request.Level,
            request.ExpiresAt);
            
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> RevokeResourcePermission(
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid permissionId,
        ISender sender)
    {
        var command = new RevokeResourcePermissionCommand(workspaceId, resourceType, resourceId, permissionId);
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> CreateShareLink(
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CreateShareLinkRequest request,
        ISender sender)
    {
        var command = new CreateShareLinkCommand(
            workspaceId,
            resourceType,
            resourceId,
            request.Level,
            request.ExpiresAt);
            
        var result = await sender.Send(command);
        return result.ToApiResult();
    }

    private static async Task<IResult> DisableShareLink(
        Guid shareLinkId,
        [FromServices] IApplicationDbContext context,
        ISender sender)
    {
        var shareLink = await context.ShareLinks
            .AsNoTracking()
            .Where(s => s.Id == shareLinkId)
            .Select(s => new { s.WorkspaceId, s.ResourceType, s.ResourceId })
            .FirstOrDefaultAsync();

        if (shareLink == null)
        {
            return Results.NotFound(new { error = $"ShareLink with ID {shareLinkId} not found." });
        }

        var command = new DisableShareLinkCommand(shareLink.WorkspaceId, shareLink.ResourceType, shareLink.ResourceId, shareLinkId);
        var result = await sender.Send(command);
        return result.ToApiResult();
    }
}

public record GrantPermissionRequest(
    string SubjectType,
    Guid SubjectId,
    string Level,
    DateTime? ExpiresAt);

public record CreateShareLinkRequest(
    string Level,
    DateTime? ExpiresAt);
