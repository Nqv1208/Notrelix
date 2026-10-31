using Notrelix.API.Contracts.Governance.Roles.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Governance.Roles;
using Notrelix.Application.Features.Governance.Roles.Commands;
using Notrelix.Application.Features.Governance.Roles.Queries;

namespace Notrelix.API.Endpoints.Governance.Roles;

/// <summary>
/// Custom roles (owner decision 2026-10-31): definitions are Account-scoped (Account Owner or Admin),
/// assignment is a workspace act of the workspace Owner, the catalog is product data.
/// </summary>
public static class MapCustomRoleEndpoints
{
    public static IEndpointRouteBuilder MapCustomRoleEndpoints_(this IEndpointRouteBuilder app)
    {
        app.MapAuthenticatedGet("/api/v1/permissions/catalog", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPermissionCatalogQuery(), ct)).ToApiResult())
            .WithName("Governance.Permissions.Catalog")
            .WithTags("Governance.Roles")
            .WithSummary("The product-defined permission catalog roles are composed from")
            .Produces<PermissionCatalogDto>();

        var roles = app.MapGroup("/api/v1/accounts/roles").WithTags("Governance.Roles").WithOpenApi();

        roles.MapAccountGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListCustomRolesQuery(), ct)).ToApiResult())
            .WithName("Governance.Roles.List")
            .WithSummary("List the account's custom roles")
            .Produces<CustomRoleListDto>();

        roles.MapAccountPost("/", async (CreateCustomRoleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new CreateCustomRoleCommand(
                    body.Name, body.Description, body.BaseRole, body.AddedActions ?? [], body.RemovedActions ?? []), ct))
                    .ToCreatedResult())
            .WithIdempotencyKey()
            .WithName("Governance.Roles.Create")
            .WithSummary("Create a custom role")
            .Produces<CustomRoleDto>(StatusCodes.Status201Created);

        roles.MapAccountPatch("/{roleId:guid}", async (Guid roleId, UpdateCustomRoleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new UpdateCustomRoleCommand(
                    roleId, body.ExpectedVersion, body.Name, body.Description, body.AddedActions ?? [], body.RemovedActions ?? []), ct))
                    .ToApiResult())
            .WithIdempotencyKey()
            .WithName("Governance.Roles.Update")
            .WithSummary("Change a custom role's name, description or actions")
            .Produces<CustomRoleDto>();

        roles.MapAccountPost("/{roleId:guid}/archive", async (Guid roleId, ArchiveCustomRoleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ArchiveCustomRoleCommand(roleId, body.ExpectedVersion, body.MoveToRoleId), ct)).ToApiResult())
            .WithIdempotencyKey()
            .WithName("Governance.Roles.Archive")
            .WithSummary("Archive a custom role, moving its holders to another role")
            .Produces<CustomRoleDto>();

        var workspace = app.MapGroup("/api/v1/workspaces/{workspaceId:guid}").WithTags("Governance.Roles").WithOpenApi();

        workspace.MapWorkspaceGet("/roles", async (Guid workspaceId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ListWorkspaceRolesQuery(workspaceId), ct)).ToApiResult())
            .WithName("Governance.Roles.ListForWorkspace")
            .WithSummary("Built-in and custom roles with the people holding them in this workspace")
            .Produces<WorkspaceRoleListDto>();

        workspace.MapWorkspaceGet("/members/me/access", async (Guid workspaceId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyEffectiveAccessQuery(workspaceId), ct)).ToApiResult())
            .WithName("Governance.Roles.MyAccess")
            .WithSummary("The caller's effective role and what a custom role adds or removes")
            .Produces<MyEffectiveAccessDto>();

        workspace.MapWorkspacePut("/members/{userId:guid}/custom-role", async (Guid workspaceId, Guid userId, AssignCustomRoleRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new AssignCustomRoleCommand(workspaceId, userId, body.RoleId), ct)).ToNoContentResult())
            .WithIdempotencyKey()
            .WithName("Governance.Roles.Assign")
            .WithSummary("Give a member a custom role (replaces their built-in role while assigned)");

        workspace.MapWorkspaceDelete("/members/{userId:guid}/custom-role", async (Guid workspaceId, Guid userId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveCustomRoleAssignmentCommand(workspaceId, userId), ct)).ToNoContentResult())
            .WithIdempotencyKey()
            .WithName("Governance.Roles.Remove")
            .WithSummary("Return a member to their built-in role");

        return app;
    }
}
