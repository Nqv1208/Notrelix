namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Canonical AccessFacts SQL authority: the provider executes EXACTLY this text and
/// performance evidence explains EXACTLY this text. Single source of truth —
/// never fork the query for tests.
/// </summary>
/// <remarks>
/// ADR-009 / GOV-025..GOV-028: <c>user_teams</c> are the active Teams of the
/// principal (Team is an evaluated subject); <c>target_space</c> is the Space the
/// resource is (a Space resource) or is placed in (a Board's <c>space_id</c>, a
/// Page's Space supplied as <c>@page_space_id</c> from the Documents-published
/// page fact); <c>space_path</c> is its ancestor-or-self path from the closure
/// index. Columns 14..17 expose the Space facts, column 18 the target user's
/// board role; every role→level meaning stays
/// in AccessPolicyEngine. GOV-033: <c>@board_id</c> is the Board the request is
/// decided on — the Board itself, or the parent Board of a Board child resource
/// (<c>@board_child</c>) supplied from the Work Management-published relation;
/// the Board's lifecycle, audience, membership, Space and grants then answer for
/// the child (permission rules scoped to the Board included), and an explicit
/// grant on the child itself still counts. Only
/// work.boards and work.board_members are read here (WG-DEBT-006 unchanged). ResourceVisibilityQuery encodes the same decision for
/// collections and is pinned to the engine by the PostgreSQL parity suite.
/// </remarks>
public static class AccessFactsQuery
{
    public const string Sql = """
        WITH user_teams AS MATERIALIZED (
          SELECT tm.team_id FROM workspace.team_members tm
            JOIN workspace.teams t ON t.id = tm.team_id
           WHERE tm.workspace_id = @workspace_id AND tm.user_id = @user_id AND tm.status = 'Active'
             AND t.workspace_id = @workspace_id AND t.deleted_at IS NULL AND t.status = 'Active'),
        target_space AS MATERIALIZED (
          SELECT CASE
            WHEN @resource_type = 'workspaces.space' THEN @resource_id
            WHEN @board_id IS NOT NULL THEN (
              SELECT b.space_id FROM work.boards b WHERE b.id = @board_id
                AND b.workspace_id = @workspace_id AND b.deleted_at IS NULL LIMIT 1)
            WHEN @resource_type = 'documents.page' THEN @page_space_id
          END AS id),
        space_path AS MATERIALIZED (
          SELECT p.ancestor_id, p.depth, s.visibility
            FROM workspace.space_paths p
            JOIN target_space ts ON p.descendant_id = ts.id
            JOIN workspace.spaces s ON s.id = p.ancestor_id
           WHERE p.workspace_id = @workspace_id)
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
            WHEN @resource_type = 'work-management.board' OR @board_child THEN
              (@resource_type = 'work-management.board' OR @resource_was_located) AND EXISTS (
              SELECT 1 FROM work.boards b WHERE b.id = @board_id
                AND b.workspace_id = @workspace_id AND b.deleted_at IS NULL AND b.is_archived = false)
            WHEN @resource_type = 'workspaces.workspace' THEN EXISTS (
              SELECT 1 FROM workspace.workspaces w WHERE w.id = @resource_id AND w.deleted_at IS NULL)
            ELSE @resource_was_located
          END,
          CASE
            WHEN @board_id IS NOT NULL THEN (
              SELECT b.visibility FROM work.boards b WHERE b.id = @board_id
                AND b.workspace_id = @workspace_id AND b.deleted_at IS NULL AND b.is_archived = false LIMIT 1)
          END,
          CASE WHEN @board_id IS NOT NULL THEN (
            SELECT bm.role FROM work.board_members bm
              WHERE bm.board_id = @board_id AND bm.user_id = @user_id LIMIT 1) END,
          EXISTS (
            SELECT 1 FROM governance.resource_permissions rp
             WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
               AND ((rp.resource_type = @resource_type AND rp.resource_id = @resource_id)
                    OR (@board_id IS NOT NULL AND rp.resource_type = 'work-management.board' AND rp.resource_id = @board_id))
               AND ((rp.subject_type = 'User' AND rp.subject_id = @user_id)
                    OR (rp.subject_type = 'Team' AND rp.subject_id IN (SELECT team_id FROM user_teams)))
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
               AND ((pr.subject_type = 'User' AND pr.subject_id = @user_id)
                    OR (pr.subject_type = 'Team' AND pr.subject_id IN (SELECT team_id FROM user_teams)))
               AND (pr.scope_type = 'Workspace'
                    OR ((pr.resource_type IS NULL OR pr.resource_type = @resource_type)
                        AND (pr.resource_id IS NULL OR pr.resource_id = @resource_id))
                    OR (@board_child AND pr.resource_type = 'work-management.board'
                        AND (pr.resource_id IS NULL OR pr.resource_id = @board_id)))
           ), '[]'::jsonb)::text,
          CASE WHEN @feature_code IS NULL THEN true ELSE COALESCE((
            SELECT e.is_unlimited OR COALESCE((
                   SELECT SUM(f.delta) FROM billing.feature_usage_ledger f
                    WHERE f.account_id = @account_id AND f.workspace_id = @workspace_id AND f.feature_code = @feature_code), 0) + @feature_amount <= e.limit_value
              FROM billing.entitlements e
             WHERE e.account_id = @account_id AND e.feature_code = @feature_code
               AND e.status = 'Active'
               AND (e.expires_at IS NULL OR e.expires_at > @now)
               AND (e.target_scope = 'Account'
                    OR (e.target_scope = 'Workspace' AND e.target_workspace_id = @workspace_id))
             ORDER BY CASE WHEN e.target_scope = 'Workspace' THEN 0 ELSE 1 END, e.created_at DESC, e.id DESC
             LIMIT 1
          ), false) END,
          CASE WHEN @resource_type IS NOT NULL THEN (
            SELECT CASE rp.permission_level
              WHEN 'Owner' THEN 5 WHEN 'Manager' THEN 4 WHEN 'Editor' THEN 3
              WHEN 'Commenter' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END
            FROM governance.resource_permissions rp
             WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
               AND ((rp.resource_type = @resource_type AND rp.resource_id = @resource_id)
                    OR (@board_id IS NOT NULL AND rp.resource_type = 'work-management.board' AND rp.resource_id = @board_id))
               AND ((rp.subject_type = 'User' AND rp.subject_id = @user_id)
                    OR (rp.subject_type = 'Team' AND rp.subject_id IN (SELECT team_id FROM user_teams)))
               AND rp.deleted_at IS NULL
             ORDER BY CASE rp.permission_level
               WHEN 'Owner' THEN 5 WHEN 'Manager' THEN 4 WHEN 'Editor' THEN 3
               WHEN 'Commenter' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END DESC
             LIMIT 1) END,
          CASE
            WHEN @target_permission_id IS NOT NULL THEN (
              SELECT CASE rp.permission_level
                WHEN 'Owner' THEN 5 WHEN 'Manager' THEN 4 WHEN 'Editor' THEN 3
                WHEN 'Commenter' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END
              FROM governance.resource_permissions rp
               WHERE rp.id = @target_permission_id
                 AND rp.workspace_id = @workspace_id AND rp.deleted_at IS NULL LIMIT 1)
            WHEN @target_subject_id IS NOT NULL THEN (
              SELECT CASE rp.permission_level
                WHEN 'Owner' THEN 5 WHEN 'Manager' THEN 4 WHEN 'Editor' THEN 3
                WHEN 'Commenter' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END
              FROM governance.resource_permissions rp
               WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
                 AND rp.resource_type = @resource_type AND rp.resource_id = @resource_id
                 AND rp.subject_type = @target_subject_type AND rp.subject_id = @target_subject_id
                 AND rp.deleted_at IS NULL
               ORDER BY CASE rp.permission_level
                 WHEN 'Owner' THEN 5 WHEN 'Manager' THEN 4 WHEN 'Editor' THEN 3
                 WHEN 'Commenter' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END DESC
               LIMIT 1)
            ELSE NULL
          END,
          (SELECT id FROM target_space),
          EXISTS (
            SELECT 1 FROM workspace.spaces s JOIN target_space ts ON s.id = ts.id
             WHERE s.workspace_id = @workspace_id AND s.deleted_at IS NULL),
          (SELECT CASE max(CASE m.role WHEN 'Admin' THEN 3 WHEN 'Member' THEN 2 WHEN 'Viewer' THEN 1 ELSE 0 END)
                    WHEN 3 THEN 'Admin' WHEN 2 THEN 'Member' WHEN 1 THEN 'Viewer' END
             FROM space_path a
             JOIN workspace.space_members m ON m.space_id = a.ancestor_id AND m.workspace_id = @workspace_id
            WHERE ((m.subject_type = 'User' AND m.subject_id = @user_id)
                   OR (m.subject_type = 'Team' AND m.subject_id IN (SELECT team_id FROM user_teams)))
              AND (m.role = 'Admin'
                   OR NOT EXISTS (SELECT 1 FROM space_path pp WHERE pp.depth < a.depth AND pp.visibility = 'Private'))),
          EXISTS (SELECT 1 FROM space_path)
            AND NOT EXISTS (SELECT 1 FROM space_path WHERE visibility = 'Private'),
          CASE WHEN @board_id IS NOT NULL AND @target_subject_type = 'User' AND @target_subject_id IS NOT NULL THEN (
            SELECT tbm.role FROM work.board_members tbm
              WHERE tbm.board_id = @board_id AND tbm.user_id = @target_subject_id LIMIT 1) END
    """;
}
