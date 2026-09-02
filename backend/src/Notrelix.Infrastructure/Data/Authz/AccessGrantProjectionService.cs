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

    public AccessGrantProjectionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SyncAccountMemberGrantAsync(
        Guid accountId,
        Guid userId,
        AccountRole role,
        DateTimeOffset now,
        CancellationToken ct)
    {
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
}