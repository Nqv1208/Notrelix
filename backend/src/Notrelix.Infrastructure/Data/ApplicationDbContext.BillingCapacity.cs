using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Infrastructure.Data;

public partial class ApplicationDbContext
{
    /// <summary>
    /// Atomic first-use seeding for a per-scope WorkspaceFeatureUsage row.
    /// The INSERT ... SELECT ... ON CONFLICT DO NOTHING is safe against a
    /// concurrent first-use race: one transaction wins the unique
    /// (account_id, workspace_id, feature_code) scope, the loser's insert
    /// blocks on the winner's uncommitted row and then becomes a no-op.
    /// CurrentUsage is materialized from the SUM of the committed ledger so
    /// first use is never allowed to drift from history, and the authoritative
    /// row is reloaded through the scoped DbSet so the winning committed state
    /// is observed before any consume/release is applied. Requires an ambient
    /// request transaction (BOUND-TX-003) so the seed commits atomically with
    /// the waiter's SaveChanges.
    /// </summary>
    public async Task<WorkspaceFeatureUsage> GetOrCreateWorkspaceFeatureUsageAsync(
        Guid accountId,
        Guid workspaceId,
        string capabilityCode,
        decimal? hardLimit,
        decimal? softLimit,
        Guid actorUserId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Atomically seeding a WorkspaceFeatureUsage requires an ambient request transaction (BOUND-TX-003).");
        }

        var id = Guid.CreateVersion7();
        var normalizedCode = capabilityCode.Trim().ToUpperInvariant();

        object?[] parameters =
        {
            id,
            accountId,
            workspaceId,
            normalizedCode,
            hardLimit,
            softLimit,
            occurredAt,
            actorUserId
        };

        await Database.ExecuteSqlRawAsync(
            """
            INSERT INTO billing.workspace_feature_usages
                (id, account_id, workspace_id, feature_code, current_usage, hard_limit, soft_limit,
                 overage_allowed, reset_period, created_at, created_by, updated_at, updated_by, version)
            SELECT @p0, @p1, @p2, @p3,
                   COALESCE((SELECT SUM(delta) FROM billing.feature_usage_ledger
                             WHERE account_id = @p1 AND workspace_id = @p2 AND feature_code = @p3), 0),
                   @p4, @p5, FALSE, 'None', @p6, @p7, @p6, @p7, 1
            ON CONFLICT (account_id, workspace_id, feature_code) DO NOTHING
            """,
            parameters!).WaitAsync(cancellationToken);

        var feature = FeatureCode.Create(normalizedCode);
        return await WorkspaceFeatureUsages.SingleOrDefaultAsync(
            w => w.AccountId == accountId
                && w.WorkspaceId == workspaceId
                && w.Feature == feature,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Failed to load the usage row for capability '{capabilityCode}' after atomic seeding.");
    }
}