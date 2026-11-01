using Notrelix.Application.Common.Data.Rls;
using Notrelix.Application.Common.Tokens;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Invitations.Services;

/// <summary>
/// Bootstrap of an invitation-token request. The one-time token is the only input of these requests
/// (no route Workspace, no member session). Where the host sets database session context, the request
/// enters the token scope: the database derives the invitation's own Account and Workspace from the
/// digest of the presented secret, and nothing outside that invitation is reachable until (for an
/// acceptance) the invited person is bound. Where it does not, the invitation is located by the digest
/// across tenants like a resource locator. A request that already runs in an explicit system context keeps it.
/// </summary>
public sealed class InvitationTokenScope
{
    private static readonly SystemOperationReason TokenReason =
        new("InvitationToken", "an invitation link is presented before the holder has any workspace access");

    private static readonly SystemOperationReason InviteeReason =
        new("InvitationToken", "the invited person accepts a pending invitation addressed to them");

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

    /// <summary>
    /// Enters the database token scope of the presented digest where the host sets session context;
    /// false when the digest opens nothing. Where there is no session context (or a system context runs)
    /// there is no scope to enter and the answer is true: locating the invitation is then the only check.
    /// </summary>
    public async Task<bool> EnterAsync(ParsedOneTimeToken token, CancellationToken ct)
    {
        if (!_rls.IsSessionContextEnabled || _tenant.IsSystemContext)
            return true;

        var binding = await _rls.EnterTokenScopeAsync(
            new TokenScopeRequest(TokenScopePurpose.WorkspaceInvitation, token.TokenHash, token.HashVersion, TokenReason),
            ct);
        return binding is not null;
    }

    /// <summary>Finds the invitation whose token hash equals the presented secret; null when none does.</summary>
    public async Task<WorkspaceInvitation?> FindAsync(ParsedOneTimeToken token, bool track, CancellationToken ct)
    {
        if (!await EnterAsync(token, ct))
            return null;

        var query = _context.WorkspaceInvitations.IgnoreQueryFilters();
        if (!track)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            i => i.Token.Value == token.TokenHash && i.HashVersion == token.HashVersion,
            ct);
    }

    /// <summary>
    /// Makes the invitation's Account and Workspace the scope of the rest of the request.
    /// <paramref name="userId"/> is the signed-in or just-registered user, null while there is none. Under a
    /// bounded database scope the token scope stays in force and an identified person is bound as the
    /// invitee (a person registered in this request is already bound by the registration scope). False
    /// only when the database refuses to bind that person as the invitee: nothing more may be read or written.
    /// </summary>
    public async Task<bool> AdoptAsync(WorkspaceInvitation invitation, Guid? userId, CancellationToken ct)
    {
        if (_tenant.IsSystemContext)
            return true;

        _tenant.SetWorkspace(invitation.AccountId, invitation.WorkspaceId, userId);

        if (_rls.ActiveBoundedScope is RlsBoundedScope.Token)
        {
            return userId is not { } person
                || await _rls.EnterInviteeScopeAsync(person, InviteeReason, ct);
        }

        if (_rls.ActiveBoundedScope is RlsBoundedScope.Invitee or RlsBoundedScope.Registration)
            return true;

        await _rls.ApplyAsync(ct);
        return true;
    }
}
