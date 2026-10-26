using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Identity.Abstractions;
using Notrelix.Application.Features.Identity.Ports.Bootstrap;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Accounts.Members;
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
                    member.Role.ToString(),
                    workspace.AccountId))
            .ToListAsync(cancellationToken);

        var accountMembers = await _accountContext.AccountMembers
            .AsNoTracking()
            .Where(m => m.UserId == authenticatedUserId && m.Status == AccountMemberStatus.Active)
            .Select(m => new { m.AccountId, m.Role })
            .ToListAsync(cancellationToken);

        var defaultWorkspaceId = await _identityContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == authenticatedUserId)
            .Select(p => p.DefaultWorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        // The Account the member acts in: that of the default Workspace, else the one the member
        // owns, else any Account membership, else the Account of a Workspace the member was invited to.
        Guid? accountId =
            workspaces.FirstOrDefault(w => w.Id == defaultWorkspaceId)?.AccountId
            ?? accountMembers.FirstOrDefault(m => m.Role == AccountRole.Owner)?.AccountId
            ?? accountMembers.FirstOrDefault()?.AccountId
            ?? workspaces.FirstOrDefault()?.AccountId;

        var ownAccountId = accountMembers.FirstOrDefault(m => m.Role == AccountRole.Owner)?.AccountId
            ?? accountMembers.FirstOrDefault()?.AccountId;
        Guid? personalWorkspaceId = null;
        if (ownAccountId is not null)
        {
            personalWorkspaceId = (await _workspaceContext.Workspaces
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    w => w.AccountId == ownAccountId && w.IsPersonal,
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
            personalWorkspaceId,
            accountId);
    }
}
