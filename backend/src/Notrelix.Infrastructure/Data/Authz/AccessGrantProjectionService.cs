using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Synchronous projection of membership facts into authz.access_grants.
/// Operates on the same scoped ApplicationDbContext as the requesting
/// handler, so granted rows commit atomically with the membership change.
/// </summary>
public sealed class AccessGrantProjectionService
{
    private const string MembershipActive = "Active";
    private const string AccountSourceContext = "Account";
    private const string WorkspaceSourceContext = "Workspace";

    private readonly ApplicationDbContext _context;
    private readonly IRlsSessionContext? _rls;
    private readonly DatabasePrincipal? _principal;

    public AccessGrantProjectionService(
        ApplicationDbContext context, IRlsSessionContext? rls = null, DatabasePrincipal? principal = null)
    {
        _context = context;
        _rls = rls;
        _principal = principal;
    }

    /// <summary>
    /// On PostgreSQL the application role has no write policy on grants,
    /// and the rest of the request needs a grant at once because its policies derive from it. The grant is
    /// therefore written immediately through the owner-defined <c>ops.upsert_access_grant</c> /
    /// <c>ops.revoke_access_grant</c> functions, which validate the write against the scope the request runs
    /// in (registration, invitee, or the request's own account and workspace) and derive the role codes and
    /// admin flags themselves. Application authorization remains the authority on whether the caller may
    /// change the membership; the functions only bound where the write can land. The background login
    /// (message consumers, for example the first-workspace provisioning) writes the rows directly: it is the
    /// unbounded principal and its policy admits them. A service built without a session context (an owner
    /// connection that seeds, or a provider without the functions) writes directly.
    /// </summary>
    private bool UsesBoundedGrantWrite =>
        _rls?.ActiveBoundedScope is RlsBoundedScope.Registration or RlsBoundedScope.Invitee
        || (_rls is not null
            && _principal is not { IsWorker: true }
            && _context.Database.IsNpgsql());

    private async Task UpsertThroughFunctionAsync(
        Guid accountId, Guid? workspaceId, Guid userId, string role, DateTimeOffset now, CancellationToken ct)
    {
        var grantedAt = now.ToUniversalTime();
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT ops.upsert_access_grant({accountId}, {workspaceId}::uuid, {userId}, {role}, {grantedAt})", ct);
    }

    public async Task SyncAccountMemberGrantAsync(
        Guid accountId,
        Guid userId,
        AccountRole role,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (UsesBoundedGrantWrite)
        {
            await UpsertThroughFunctionAsync(accountId, null, userId, role.ToString(), now, ct);
            return;
        }

        // Track pending Adds too: the sync must stay idempotent when invoked
        // more than once per transaction (already-active membership is a
        // contract-level no-op), and a DB query alone cannot see unsaved adds.
        var grant = _context.AccessGrants.Local
            .FirstOrDefault(
                g => g.AccountId == accountId && g.UserId == userId && g.WorkspaceId == null)
            ?? await _context.AccessGrants
            .FirstOrDefaultAsync(
                g => g.AccountId == accountId && g.UserId == userId && g.WorkspaceId == null,
                ct);

        if (grant is null)
        {
            _context.AccessGrants.Add(new AccessGrant(
                accountId,
                null,
                userId,
                AccountSourceContext,
                MembershipActive,
                AccessGrantProjectionMapping.RoleCodes(role),
                [],
                AccessGrantProjectionMapping.IsAccountAdmin(role),
                false,
                now));
            return;
        }

        grant.Activate(
            MembershipActive,
            AccessGrantProjectionMapping.RoleCodes(role),
            [],
            AccessGrantProjectionMapping.IsAccountAdmin(role),
            false,
            now);
    }

    public async Task SyncWorkspaceMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        WorkspaceRole role,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (UsesBoundedGrantWrite)
        {
            await UpsertThroughFunctionAsync(accountId, workspaceId, userId, role.ToString(), now, ct);
            return;
        }

        var grant = _context.AccessGrants.Local
            .FirstOrDefault(
                g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId)
            ?? await _context.AccessGrants
            .FirstOrDefaultAsync(
                g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId,
                ct);

        if (grant is null)
        {
            _context.AccessGrants.Add(new AccessGrant(
                accountId,
                workspaceId,
                userId,
                WorkspaceSourceContext,
                MembershipActive,
                AccessGrantProjectionMapping.RoleCodes(role),
                [],
                false,
                AccessGrantProjectionMapping.IsWorkspaceAdmin(role),
                now));
            return;
        }

        grant.Activate(
            MembershipActive,
            AccessGrantProjectionMapping.RoleCodes(role),
            [],
            false,
            AccessGrantProjectionMapping.IsWorkspaceAdmin(role),
            now);
    }

    public async Task RevokeWorkspaceMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (UsesBoundedGrantWrite)
        {
            var revokedAt = now.ToUniversalTime();
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT ops.revoke_access_grant({accountId}, {workspaceId}, {userId}, {revokedAt})", ct);
            return;
        }

        await RevokeTrackedGrantAsync(accountId, workspaceId, userId, now, ct);
    }

    private async Task RevokeTrackedGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var grant = await _context.AccessGrants
            .FirstOrDefaultAsync(
                g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId,
                ct);

        grant?.Revoke(now);
    }

    /// <summary>
    /// The leaver's own grant is the access that lets them update their membership row under RLS, so the
    /// pending membership change is flushed first (same open request transaction; the pipeline still owns
    /// the commit) and the grant is revoked afterwards through the narrow own-grant update policy (the one
    /// grant write the application role holds). Failure of either statement rolls back both.
    /// </summary>
    public async Task RevokeLeavingMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
        await RevokeTrackedGrantAsync(accountId, workspaceId, userId, now, ct);
    }

    /// <summary>
    /// Ends the account-level Member grant of a person who holds no active grant in any other Workspace of
    /// the Account. The bounded function decides under the request's scope (the person themself or a
    /// workspace administrator); a service without a session context writes the tracked rows directly.
    /// An account administrator's grant is never touched.
    /// </summary>
    public async Task RevokeAccountGrantWhenLastWorkspaceLeftAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (UsesBoundedGrantWrite)
        {
            var revokedAt = now.ToUniversalTime();
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT ops.revoke_account_grant_if_last_workspace({accountId}, {workspaceId}, {userId}, {revokedAt})", ct);
            return;
        }

        var stillMember = await _context.AccessGrants.AnyAsync(
            g => g.AccountId == accountId && g.UserId == userId && g.WorkspaceId != null && g.WorkspaceId != workspaceId
                && g.RevokedAt == null && g.MembershipStatus == MembershipActive, ct);
        if (stillMember) return;

        var accountGrant = await _context.AccessGrants.FirstOrDefaultAsync(
            g => g.AccountId == accountId && g.UserId == userId && g.WorkspaceId == null
                && g.RevokedAt == null && !g.IsAccountAdmin, ct);
        accountGrant?.Revoke(now);
    }

    /// <summary>Ends the custom-role assignments of one member of the Workspace (removed or reactivated).</summary>
    public async Task ClearCustomRoleAssignmentsAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        CancellationToken ct)
    {
        if (UsesBoundedGrantWrite)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT ops.clear_member_custom_roles({accountId}, {workspaceId}, {userId})", ct);
            return;
        }

        // MemberId is the person's user id.
        var assignments = await _context.MemberRoleAssignments
            .Where(a => a.AccountId == accountId && a.WorkspaceId == workspaceId && a.MemberId == userId)
            .ToListAsync(ct);
        _context.MemberRoleAssignments.RemoveRange(assignments);
    }
}
