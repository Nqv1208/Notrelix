namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Canonical AccessFacts SQL authority: the provider executes EXACTLY this text and
/// performance evidence explains EXACTLY this text. Single source of truth —
/// never fork the query for tests.
/// </summary>
public static class AccessFactsQuery
{
    public const string Sql = """
        SELECT
          EXISTS (SELECT 1 FROM identity.users u WHERE u.id = @user_id AND u.deleted_at IS NULL),
          COALESCE((SELECT u.email_confirmed FROM identity.users u WHERE u.id = @user_id AND u.deleted_at IS NULL), false),
          EXISTS (SELECT 1 FROM account.accounts a WHERE a.id = @account_id AND a.deleted_at IS NULL),
          (SELECT am.role FROM account.account_members am
             WHERE am.account_id = @account_id AND am.user_id = @user_id
               AND am.status = 'Active' AND am.deleted_at IS NULL LIMIT 1),
          EXISTS (SELECT 1 FROM workspace.workspaces w WHERE w.id = @workspace_id AND w.deleted_at IS NULL),
          (SELECT wm.role FROM workspace.workspace_members wm
             WHERE wm.account_id = @account_id AND wm.workspace_id = @workspace_id
               AND wm.user_id = @user_id AND wm.status = 'Active' LIMIT 1),
          CASE
            WHEN @resource_type = 'work-management.board' THEN EXISTS (
              SELECT 1 FROM work.boards b WHERE b.id = @resource_id
                AND b.workspace_id = @workspace_id AND b.deleted_at IS NULL AND b.is_archived = false)
            WHEN @resource_type = 'workspaces.workspace' THEN EXISTS (
              SELECT 1 FROM workspace.workspaces w WHERE w.id = @resource_id AND w.deleted_at IS NULL)
            ELSE @resource_was_located
          END,
          CASE WHEN @resource_type = 'work-management.board' THEN (
            SELECT b.visibility FROM work.boards b WHERE b.id = @resource_id
              AND b.deleted_at IS NULL AND b.is_archived = false LIMIT 1) END,
          CASE WHEN @resource_type = 'work-management.board' THEN (
            SELECT bm.role FROM work.board_members bm
              WHERE bm.board_id = @resource_id AND bm.user_id = @user_id LIMIT 1) END,
          EXISTS (
            SELECT 1 FROM governance.resource_permissions rp
             WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
               AND rp.resource_type = @resource_type AND rp.resource_id = @resource_id
               AND rp.subject_type = 'User' AND rp.subject_id = @user_id
               AND rp.deleted_at IS NULL),
          COALESCE((
            SELECT jsonb_agg(jsonb_build_object('priority', pr.priority, 'effect', pr.effect) ORDER BY pr.priority)
              FROM governance.permission_rules pr
             WHERE @workspace_id IS NOT NULL
               AND pr.account_id = @account_id AND pr.workspace_id = @workspace_id
               AND pr.status = 'Active' AND pr.deleted_at IS NULL
               AND (pr.starts_at IS NULL OR pr.starts_at <= @now)
               AND (pr.expires_at IS NULL OR pr.expires_at > @now)
               AND pr.action = @action
               AND pr.subject_type = 'User' AND pr.subject_id = @user_id
               AND (pr.scope_type = 'Workspace'
                    OR ((pr.resource_type IS NULL OR pr.resource_type = @resource_type)
                        AND (pr.resource_id IS NULL OR pr.resource_id = @resource_id)))
          ), '[]'::jsonb)::text,
          EXISTS (
            SELECT 1 FROM billing.subscriptions s
             WHERE s.account_id = @account_id AND s.status = 'Active' AND s.current_period_end > @now),
          (SELECT s.tier FROM billing.subscriptions s
             WHERE s.account_id = @account_id AND s.status = 'Active' AND s.current_period_end > @now
             ORDER BY CASE s.tier
               WHEN 'Enterprise' THEN 5 WHEN 'Business' THEN 4 WHEN 'Pro' THEN 3
               WHEN 'Starter' THEN 2 ELSE 1 END DESC LIMIT 1),
          CASE WHEN @feature_code IS NULL THEN true ELSE COALESCE((
            SELECT e.status = 'Active'
               AND (e.expires_at IS NULL OR e.expires_at > @now)
               AND (e.limit_value = 0 OR COALESCE((
                   SELECT SUM(f.delta) FROM billing.feature_usage_ledger f
                    WHERE f.account_id = @account_id AND f.feature_code = @feature_code), 0) + @feature_amount <= e.limit_value)
              FROM billing.entitlements e
             WHERE e.account_id = @account_id AND e.feature_code = @feature_code
             ORDER BY e.created_at DESC LIMIT 1
          ), false) END,
          EXISTS (SELECT 1 FROM account.accounts a
                   WHERE a.id = @account_id AND a.deleted_at IS NULL
                     AND a.status IN ('Active', 'Trialing')),
          EXISTS (SELECT 1 FROM identity.users u
                   WHERE u.id = @user_id AND u.deleted_at IS NULL
                     AND u.status IN ('Active', 'PendingVerification'))
    """;
}
