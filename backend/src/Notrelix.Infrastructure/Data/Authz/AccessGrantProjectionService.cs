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

    public AccessGrantProjectionService(ApplicationDbContext context, IRlsSessionContext? rls = null)
    {
        _context = context;
        _rls = rls;
    }

    /// <summary>
    /// Inside a registration or invitee scope the grant is written immediately through the owner-defined
    /// <c>ops.upsert_access_grant</c> function (the application role has no write policy on grants, and the
    /// rest of the request needs the grant at once because its policies derive from it). The function
    /// validates the write against the scope and derives the role codes and admin flags itself.
    /// </summary>
    private bool UsesBoundedGrantWrite =>
        _rls?.ActiveBoundedScope is RlsBoundedScope.Registration or RlsBoundedScope.Invitee;

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
        var grant = await _context.AccessGrants
            .FirstOrDefaultAsync(
                g => g.AccountId == accountId && g.WorkspaceId == workspaceId && g.UserId == userId,
                ct);

        grant?.Revoke(now);
    }

    /// <summary>
    /// The leaver's own grant is the access that lets them update their membership row under RLS, so the
    /// pending membership change is flushed first (same open request transaction; the pipeline still owns
    /// the commit) and the grant is revoked afterwards. Failure of either statement rolls back both.
    /// </summary>
    public async Task RevokeLeavingMemberGrantAsync(
        Guid accountId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
        await RevokeWorkspaceMemberGrantAsync(accountId, workspaceId, userId, now, ct);
    }
}
