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
                    workspace.AccountId))
            .ToListAsync(cancellationToken);

        var memberRows = await _accountContext.AccountMembers
            .AsNoTracking()
            .Where(m => m.UserId == authenticatedUserId && m.Status == AccountMemberStatus.Active)
            .Select(m => new { m.AccountId, m.Role })
            .ToListAsync(cancellationToken);

        // The Account names only break a tie between memberships, so they are read for these Accounts alone.
        // A membership whose Account row is not visible keeps its place, with an empty name.
        var memberAccountIds = memberRows.Select(m => m.AccountId).Distinct().ToList();
        var accountNames = memberAccountIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _accountContext.Accounts
                .AsNoTracking()
                .Where(a => memberAccountIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);
        var accountMembers = memberRows
            .Select(m => new BootstrapAccountMembership(
                m.AccountId, m.Role, accountNames.GetValueOrDefault(m.AccountId, string.Empty)))
            .ToList();

        var defaultWorkspaceId = await _identityContext.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == authenticatedUserId)
            .Select(p => p.DefaultWorkspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        // The Account the member acts in, chosen by the stable order of BootstrapAccountChoice (default
        // Workspace's Account, then an owned Account, then any, then a Workspace's Account), never by the
        // order the rows came back in.
        var accountId = BootstrapAccountChoice.Choose(defaultWorkspaceId, workspaces, accountMembers);

        var ownAccountId = BootstrapAccountChoice.Own(accountMembers);
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
