using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Ports.Bootstrap;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Infrastructure.CrossContext.Identity.Bootstrap;

/// <summary>
/// Executes the bootstrap read projection across Identity, Workspaces and
/// Accounts. The caller's identity is never taken from request input — it is
/// always the authenticated current user supplied by the query.
/// </summary>
public sealed class IdentityBootstrapReadAdapter : IIdentityBootstrapReadPort
{
    private readonly IIdentityDbContext _identityContext;
    private readonly IWorkspaceDbContext _workspaceContext;
    private readonly IAccountDbContext _accountContext;

    public IdentityBootstrapReadAdapter(
        IIdentityDbContext identityContext,
        IWorkspaceDbContext workspaceContext,
        IAccountDbContext accountContext)
    {
        _identityContext = identityContext;
        _workspaceContext = workspaceContext;
        _accountContext = accountContext;
    }

    public async Task<IdentityBootstrapProjection?> GetAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken)
    {
        var user = await _identityContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == authenticatedUserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        // The bootstrap is a global request: it has no Workspace, and the EF tenant filter of the
        // workspace-scoped sets keys on the request Workspace, so it would hide every membership. The
        // subject is fixed to the authenticated user; the soft-delete condition is stated explicitly.
        var workspaces = await _workspaceContext.WorkspaceMembers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(m => m.UserId == authenticatedUserId && m.Status == WorkspaceMemberStatus.Active)
            .Join(_workspaceContext.Workspaces.IgnoreQueryFilters().Where(w => w.DeletedAt == null),
                member => member.WorkspaceId,
                workspace => workspace.Id,
                (member, workspace) => new BootstrapWorkspaceProjection(
                    workspace.Id,
                    workspace.Name,
                    workspace.Slug,
                    member.Role.ToString()))
            .ToListAsync(cancellationToken);

        var accountMember = await _accountContext.AccountMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == authenticatedUserId, cancellationToken);

        Guid? personalWorkspaceId = null;
        if (accountMember is not null)
        {
            personalWorkspaceId = (await _workspaceContext.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.AccountId == accountMember.AccountId && w.IsPersonal,
                    cancellationToken))?.Id;
        }

        return new IdentityBootstrapProjection(
            new BootstrapUserProjection(
                user.Id,
                user.Email.Value,
                user.Name,
                user.AvatarUrl,
                user.EmailConfirmed),
            workspaces,
            personalWorkspaceId);
    }
}
