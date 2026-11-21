using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Documents.Templates;

namespace Notrelix.Application.Features.Documents.Templates.Services;

/// <summary>
/// Who may rename, archive or delete a template: the person who saved it, or an active Workspace Owner or Admin
/// (the template lifecycle is separate from the page lifecycle). Anyone else in the workspace can see the
/// template and is refused (403).
/// </summary>
internal static class PageTemplateManagers
{
    public static async Task<bool> CanManageAsync(
        IWorkspaceMemberFacts members, Guid workspaceId, PageTemplate template, Guid userId, CancellationToken ct)
    {
        if (template.CreatedBy == userId) return true;
        var member = await members.FindAsync(workspaceId, userId, ct);
        return member is { IsActive: true, Role: "Owner" or "Admin" };
    }

    public static async Task RequireAsync(
        IWorkspaceMemberFacts members, Guid workspaceId, PageTemplate template, Guid userId, CancellationToken ct)
    {
        if (!await CanManageAsync(members, workspaceId, template, userId, ct))
            throw new ForbiddenException("Only the person who saved a template or a Workspace Admin can change it.");
    }
}
