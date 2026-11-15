-- =============================================================================
-- 030_chat_policy_fast_path.sql — Chat tables: one grant lookup per statement instead of one per row
-- =============================================================================
-- Why: the chat policies (008) call ops.has_workspace_access(account_id, workspace_id) for every row a statement
-- touches. The function is SECURITY DEFINER with SET search_path and calls three more such functions, so it cannot
-- be inlined and costs about 0.12 ms per row. Counting the unread messages of one person across 60 channels read
-- 11,000 rows and spent 1.4 s in that call; the same statement without the policy takes 26 ms.
--
-- What: the policy now reads
--     (account_id, workspace_id) IN (SELECT ... FROM ops.accessible_workspace_pairs())   -- hashed, once per statement
--     OR ops.has_workspace_access(account_id, workspace_id)                              -- the original rule, unchanged
-- ops.accessible_workspace_pairs() lists the (account, workspace) pairs of the caller's active workspace grants:
-- exactly the rows for which ops.has_workspace_access() answers true through its grant branch (026), with the same
-- conditions (same user setting, revoked_at IS NULL, membership_status = 'Active'). A pair in that list is therefore
-- always one the original function accepts, so the first arm can only short-circuit a true answer; every row the
-- function accepts for another reason (worker scope, invitee scope, NULL arguments) still reaches it through the
-- second arm. The set of visible rows does not change, and no row becomes visible that was not before.
--
-- Same SECURITY DEFINER, search_path pinning and grants as the helpers it sits beside. Tables, commands and roles are
-- exactly the ones 008 gives the chat tables (ops.apply_scope_expression_policies keeps the worker and support
-- policies and the app write policies).
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.accessible_workspace_pairs()
RETURNS TABLE (account_id uuid, workspace_id uuid)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, authz, ops, pg_temp
AS $$
    SELECT g.account_id, g.workspace_id
    FROM authz.access_grants g
    WHERE g.user_id = ops.current_user_id()
      AND g.account_id IS NOT NULL
      AND g.workspace_id IS NOT NULL
      AND g.revoked_at IS NULL
      AND g.membership_status = 'Active';
$$;

REVOKE ALL ON FUNCTION ops.accessible_workspace_pairs() FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.accessible_workspace_pairs() TO notrelix_app, notrelix_worker, notrelix_support_readonly, notrelix_migrator;

DO $$
DECLARE
    t text;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'spaces', 'channel_groups', 'memberships', 'messages', 'threads', 'thread_followers', 'pins',
        'reactions', 'message_mentions', 'permission_overrides', 'direct_pairs', 'blocks',
        'retention_policies', 'export_jobs'
    ]
    LOOP
        PERFORM ops.apply_scope_expression_policies(
            'chat', t,
            '(account_id, workspace_id) IN (SELECT p.account_id, p.workspace_id FROM ops.accessible_workspace_pairs() p) '
            || 'OR ops.has_workspace_access(account_id, workspace_id)',
            true);
    END LOOP;
END
$$;
