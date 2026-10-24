using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// Bootstrap of an invitation-token request. The one-time token is the only input of these requests
/// (no route Workspace, no member session), so the invitation is located by the hash of the secret
/// across tenants, exactly like a resource locator, and the request then adopts the Workspace scope
/// named by that invitation for the rest of its transaction. The adopted scope is the invitation's own
/// Account and Workspace: nothing is widened beyond what the presented secret proves. A request that
/// already runs in an explicit system context keeps it.
/// </summary>
public sealed class InvitationTokenScope
{
    private readonly IWorkspaceDbContext _context;
    private readonly ICurrentTenantContext _tenant;
    private readonly IRlsSessionContext _rls;

    public InvitationTokenScope(
        IWorkspaceDbContext context,
        ICurrentTenantContext tenant,
        IRlsSessionContext rls)
    {
        _context = context;
        _tenant = tenant;
        _rls = rls;
    }

    /// <summary>Finds the invitation whose token hash equals the presented secret; null when none does.</summary>
    public Task<WorkspaceInvitation?> FindAsync(ParsedOneTimeToken token, bool track, CancellationToken ct)
    {
        var query = _context.WorkspaceInvitations.IgnoreQueryFilters();
        if (!track)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(
            i => i.Token.Value == token.TokenHash && i.HashVersion == token.HashVersion,
            ct);
    }

    /// <summary>
    /// Makes the invitation's Account and Workspace the scope of the rest of the request.
    /// <paramref name="userId"/> is the signed-in or just-registered user, null while there is none.
    /// </summary>
    public async Task AdoptAsync(WorkspaceInvitation invitation, Guid? userId, CancellationToken ct)
    {
        if (_tenant.IsSystemContext)
            return;

        _tenant.SetWorkspace(invitation.AccountId, invitation.WorkspaceId, userId);
        await _rls.ApplyAsync(ct);
    }
}
