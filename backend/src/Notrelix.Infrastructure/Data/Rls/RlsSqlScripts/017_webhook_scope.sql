-- =============================================================================
-- 017_webhook_scope.sql — Bounded scope of a verified inbound calendar webhook
-- =============================================================================
-- A provider callback authenticates by signature, not by a person, so it has no user whose grants a policy
-- could read. After the handler verified the signature it enters a scope bound to the tenant of the calendar
-- binding the route's webhook path identifies; the database derives that tenant itself from the path, so the
-- application never supplies identifiers it could be dishonest about. The scope covers exactly the two
-- INSERTs the intake performs (the "Captured" receipt and the processing message in the outbox) and the
-- receipt read-back of the claim, for that one account and workspace. It is a token-kind scope (never a
-- member of ops.is_worker_scope()), carries the purpose 'calendar-webhook' so no other token policy admits it,
-- and ends with the transaction. The tenant-scoped processing consumer runs afterwards on the background
-- principal.
-- =============================================================================

CREATE OR REPLACE FUNCTION ops.has_webhook_binding(p_account uuid, p_workspace uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_account IS NOT NULL
       AND p_workspace IS NOT NULL
       AND ops.current_request_scope() = 'token'
       AND ops.current_setting_text('app.scope_token_purpose') = 'calendar-webhook'
       AND p_account = ops.current_setting_uuid('app.scope_account_id')
       AND p_workspace = ops.current_setting_uuid('app.scope_workspace_id');
$$;

-- Enters the webhook scope for the active, undeleted calendar binding with this webhook path. Returns the
-- derived account and workspace, or nothing (no scope entered) when no such binding exists. Must run inside
-- a transaction: the values are transaction-local.
CREATE OR REPLACE FUNCTION ops.enter_webhook_scope(p_webhook_path text)
RETURNS TABLE (account_id uuid, workspace_id uuid)
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
DECLARE
    v_account uuid;
    v_workspace uuid;
BEGIN
    IF p_webhook_path IS NULL OR length(p_webhook_path) = 0 THEN
        RETURN;
    END IF;

    SELECT c.account_id, c.workspace_id
      INTO v_account, v_workspace
      FROM integration.calendar_integrations c
     WHERE c.webhook_path = p_webhook_path
       AND c.deleted_at IS NULL
       AND c.is_active;

    IF v_account IS NULL OR v_workspace IS NULL THEN
        RETURN;
    END IF;

    PERFORM set_config('app.request_scope', 'token', true);
    PERFORM set_config('app.scope_token_purpose', 'calendar-webhook', true);
    PERFORM set_config('app.scope_token_hash', '', true);
    PERFORM set_config('app.scope_account_id', v_account::text, true);
    PERFORM set_config('app.scope_workspace_id', v_workspace::text, true);
    PERFORM set_config('app.scope_user_id', '', true);
    PERFORM set_config('app.scope_invitee_id', '', true);
    PERFORM set_config('app.current_user_id', '', true);
    PERFORM set_config('app.current_account_id', v_account::text, true);
    PERFORM set_config('app.current_workspace_id', v_workspace::text, true);

    RETURN QUERY SELECT v_account, v_workspace;
END;
$$;

REVOKE ALL ON FUNCTION ops.has_webhook_binding(uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.enter_webhook_scope(text) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.has_webhook_binding(uuid, uuid) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.enter_webhook_scope(text) TO notrelix_app;

-- The intake claims the delivery (INSERT ... RETURNING) and enqueues the processing message, for the bound
-- tenant only. Nothing else is readable, updatable or deletable in this scope.
SELECT ops.create_policy(
    'integration', 'inbound_webhook_receipts', 'p_app_select_webhook_binding', 'SELECT', 'notrelix_app',
    'ops.has_webhook_binding(account_id, workspace_id)', NULL);
SELECT ops.create_policy(
    'integration', 'inbound_webhook_receipts', 'p_app_insert_webhook_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_webhook_binding(account_id, workspace_id)');
SELECT ops.create_policy(
    'messaging', 'outbox_messages', 'p_app_insert_webhook_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_webhook_binding(account_id, workspace_id)');
