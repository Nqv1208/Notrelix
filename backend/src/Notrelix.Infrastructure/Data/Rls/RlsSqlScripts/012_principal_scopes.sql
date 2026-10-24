-- =============================================================================
-- 012_principal_scopes.sql — Bounded principal scopes (user, token)
-- =============================================================================
-- Scope values written here are NOT members of ops.is_worker_scope(): a bounded scope never satisfies the
-- unbounded worker bypass. The scope-bound session values (app.scope_*) are written by the application
-- connection for the authenticated user scope and ONLY by the enter_* definer functions for token scopes,
-- which derive account/workspace from a presented token digest instead of trusting supplied identifiers.
-- Entry functions must run inside a transaction: the values are transaction-local.
-- =============================================================================

-- True when the row's user is the user the current bounded scope was entered for.
CREATE OR REPLACE FUNCTION ops.has_user_scope(p_user uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_user IS NOT NULL
       AND ops.current_request_scope() IN ('user', 'credential', 'registration')
       AND p_user = ops.current_setting_uuid('app.scope_user_id');
$$;

-- True when the row belongs to the account (and workspace, when the row has one) the current token scope
-- was entered for. Never true in any other scope.
CREATE OR REPLACE FUNCTION ops.has_token_scope(p_account uuid, p_workspace uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p_account IS NOT NULL
       AND ops.current_request_scope() = 'token'
       AND p_account = ops.current_setting_uuid('app.scope_account_id')
       AND (p_workspace IS NULL OR p_workspace = ops.current_setting_uuid('app.scope_workspace_id'));
$$;

-- Enters the token scope named by a presented one-time token. The token row is located by digest (the
-- function owner bypasses row-level security for this single lookup); when found, the scope is bound to
-- the token's own account and workspace and those two identifiers are returned. When no row matches, no
-- scope is entered and nothing is returned. The digest, never a raw token, is the input. The purpose
-- ('workspace-invitation', 'api-token') is recorded with the scope so a policy written for one kind of token
-- never admits the scope of another.
CREATE OR REPLACE FUNCTION ops.enter_token_scope(p_purpose text, p_token_hash text, p_hash_version integer)
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
    IF p_token_hash IS NULL OR length(p_token_hash) = 0 THEN
        RETURN;
    END IF;

    IF p_purpose = 'workspace-invitation' THEN
        SELECT i.account_id, i.workspace_id
          INTO v_account, v_workspace
          FROM workspace.workspace_invitations i
         WHERE i.token_hash = p_token_hash
           AND i.hash_version = p_hash_version;
    ELSIF p_purpose = 'api-token' THEN
        -- Only a token that could authenticate right now enters a scope: active, not expired, and bound to
        -- a user. A revoked, expired, user-less or unknown token enters nothing.
        SELECT t.account_id, t.workspace_id
          INTO v_account, v_workspace
          FROM identity.api_tokens t
         WHERE t.token_hash = p_token_hash
           AND t.status = 'Active'
           AND t.user_id IS NOT NULL
           AND (t.expires_at IS NULL OR t.expires_at > now());
    ELSE
        RAISE EXCEPTION 'Unknown token scope purpose' USING ERRCODE = '22023';
    END IF;

    IF v_account IS NULL OR v_workspace IS NULL THEN
        RETURN;
    END IF;

    PERFORM set_config('app.request_scope', 'token', true);
    PERFORM set_config('app.scope_account_id', v_account::text, true);
    PERFORM set_config('app.scope_workspace_id', v_workspace::text, true);
    PERFORM set_config('app.scope_user_id', '', true);
    PERFORM set_config('app.scope_token_hash', p_token_hash, true);
    PERFORM set_config('app.scope_token_purpose', p_purpose, true);
    PERFORM set_config('app.scope_invitee_id', '', true);
    PERFORM set_config('app.scope_invitation_role', '', true);
    PERFORM set_config('app.scope_invitation_email', '', true);
    PERFORM set_config('app.scope_registration_account_id', '', true);

    RETURN QUERY SELECT v_account, v_workspace;
END;
$$;

REVOKE ALL ON FUNCTION ops.has_user_scope(uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.has_token_scope(uuid, uuid) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.enter_token_scope(text, text, integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.has_user_scope(uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.has_token_scope(uuid, uuid) TO notrelix_app, notrelix_auth, notrelix_worker, notrelix_support_readonly;
GRANT EXECUTE ON FUNCTION ops.enter_token_scope(text, text, integer) TO notrelix_app;

-- -----------------------------------------------------------------------------
-- Tenantless internal rows written by Global authenticated requests.
-- A Global request (profile, sessions, MFA ...) raises domain events that carry no account or workspace.
-- The tenant-scoped INSERT policies of these internal tables cannot admit such a row, so a second
-- INSERT-only policy admits exactly it, and only in the user scope of the user who raised it. No read,
-- update or delete is added: the tables stay worker-readable only.
-- -----------------------------------------------------------------------------
SELECT ops.create_policy(
    'events', 'domain_event_logs', 'p_app_insert_user_scope', 'INSERT', 'notrelix_app', NULL,
    'workspace_id IS NULL AND ops.has_user_scope(ops.current_user_id()) '
    || 'AND (actor_user_id IS NULL OR actor_user_id = ops.current_user_id())');
SELECT ops.create_policy(
    'messaging', 'outbox_messages', 'p_app_insert_user_scope', 'INSERT', 'notrelix_app', NULL,
    'account_id IS NULL AND workspace_id IS NULL AND ops.has_user_scope(ops.current_user_id())');
