-- =============================================================================
-- 015_internal_principals.sql — Internal principals and bounded reads/writes
-- =============================================================================
-- The unbounded background principal is the notrelix_worker LOGIN (its own connection; see
-- ops.is_worker_scope() in 002). Everything here is for the application connection:
--
--   api-token scope          an API token that authenticates enters a scope bound to ITS account and
--                            workspace (012: enter_token_scope, purpose 'api-token'); the policies below
--                            let it read and record use of that one token row and nothing else.
--   security events          tenantless and tenant audit rows are written through ONE insert-only function.
--   others' email and phone  owner-defined reads that mirror the Application disclosure rules.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- API token scope
-- -----------------------------------------------------------------------------
-- True when the current scope is the api-token scope entered for this account and/or workspace (a NULL
-- argument is not compared, but not both may be NULL) and the digest the scope was entered with. Never true
-- for an invitation token scope (the purpose differs) or any other scope.
CREATE OR REPLACE FUNCTION ops.has_api_token_binding(p_account uuid, p_workspace uuid)
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT ops.current_request_scope() = 'token'
       AND ops.current_setting_text('app.scope_token_purpose') = 'api-token'
       AND ops.current_setting_text('app.scope_token_hash') IS NOT NULL
       AND (p_account IS NULL OR p_account = ops.current_setting_uuid('app.scope_account_id'))
       AND (p_workspace IS NULL OR p_workspace = ops.current_setting_uuid('app.scope_workspace_id'))
       AND COALESCE(p_account, p_workspace) IS NOT NULL;
$$;
REVOKE ALL ON FUNCTION ops.has_api_token_binding(uuid, uuid) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.has_api_token_binding(uuid, uuid) TO notrelix_app;

-- Recording the use of a token raises a domain event, whose internal log and outbox rows are written by the
-- same request. INSERT only (the tables stay unreadable to the application role), and only for the tenant the
-- scope is bound to.
SELECT ops.create_policy(
    'events', 'domain_event_logs', 'p_app_insert_api_token_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_api_token_binding(NULL, workspace_id)');
SELECT ops.create_policy(
    'messaging', 'outbox_messages', 'p_app_insert_api_token_binding', 'INSERT', 'notrelix_app', NULL,
    'ops.has_api_token_binding(account_id, workspace_id)');

-- The authentication step reads the one token row it entered the scope for and records its use. It never
-- reads or writes another token, and it needs no INSERT or DELETE.
SELECT ops.create_policy(
    'identity', 'api_tokens', 'p_app_select_api_token_binding', 'SELECT', 'notrelix_app',
    'ops.has_api_token_binding(account_id, workspace_id) AND token_hash = ops.current_setting_text(''app.scope_token_hash'')',
    NULL);
SELECT ops.create_policy(
    'identity', 'api_tokens', 'p_app_update_api_token_binding', 'UPDATE', 'notrelix_app',
    'ops.has_api_token_binding(account_id, workspace_id) AND token_hash = ops.current_setting_text(''app.scope_token_hash'')',
    'ops.has_api_token_binding(account_id, workspace_id) AND token_hash = ops.current_setting_text(''app.scope_token_hash'')');

-- -----------------------------------------------------------------------------
-- Security events
-- -----------------------------------------------------------------------------
-- The only way the application role (or the worker) inserts an audit.security_events row. A security event
-- is often tenantless (an anonymous or pre-authentication failure), which no tenant-scoped policy admits,
-- so the row is written by this owner-defined, insert-only function. The caller supplies the facts of the
-- event; the ACTOR and the TENANT are not parameters:
--   user_id      the user of the current scope (app, user, credential, registration), else NULL;
--   workspace_id the workspace of the current tenant (app) scope when there is one, else NULL.
-- The event type must be one of the known set. No SELECT, UPDATE or DELETE is granted by this function or
-- by any policy added for it.
CREATE OR REPLACE FUNCTION ops.record_security_event(
    p_event_type text,
    p_severity text,
    p_outcome text,
    p_risk_score integer,
    p_ip_address text,
    p_user_agent text,
    p_device_id text,
    p_session_id uuid,
    p_resource_type text,
    p_resource_id uuid,
    p_correlation_id text,
    p_metadata jsonb,
    p_occurred_at timestamptz)
RETURNS void
LANGUAGE plpgsql
VOLATILE
SECURITY DEFINER
SET search_path = pg_catalog, audit, ops, pg_temp
AS $$
DECLARE
    v_scope text := ops.current_request_scope();
    v_user uuid;
    v_workspace uuid;
BEGIN
    IF p_event_type IS NULL OR p_event_type NOT IN
        ('LoginFailed', 'UnauthorizedAccess', 'ForbiddenAccess', 'RateLimitExceeded', 'SecurityObservation') THEN
        RAISE EXCEPTION 'Unknown security event type' USING ERRCODE = '22023';
    END IF;
    IF p_severity IS NULL OR p_severity NOT IN ('Info', 'Warning', 'Critical') THEN
        RAISE EXCEPTION 'Unknown security event severity' USING ERRCODE = '22023';
    END IF;
    IF p_outcome IS NULL OR length(p_outcome) = 0 OR length(p_outcome) > 40 THEN
        RAISE EXCEPTION 'Invalid security event outcome' USING ERRCODE = '22023';
    END IF;
    IF p_risk_score IS NOT NULL AND (p_risk_score < 0 OR p_risk_score > 100) THEN
        RAISE EXCEPTION 'Invalid security event risk score' USING ERRCODE = '22023';
    END IF;
    IF p_occurred_at IS NULL THEN
        RAISE EXCEPTION 'A security event needs its time' USING ERRCODE = '22023';
    END IF;

    IF v_scope IN ('app', 'user', 'credential', 'registration') THEN
        v_user := ops.current_user_id();
    END IF;
    IF v_scope = 'app' THEN
        v_workspace := ops.current_workspace_id();
    END IF;

    INSERT INTO audit.security_events (
        id, workspace_id, user_id, event_type, severity, outcome, risk_score, ip_address, user_agent,
        device_id, session_id, resource_type, resource_id, correlation_id, metadata_json, occurred_at, recorded_at)
    VALUES (
        gen_random_uuid(), v_workspace, v_user, p_event_type, p_severity, p_outcome, p_risk_score, p_ip_address,
        p_user_agent, left(p_device_id, 160), p_session_id, left(p_resource_type, 160), p_resource_id,
        left(p_correlation_id, 100), COALESCE(p_metadata, '{}'::jsonb), p_occurred_at, p_occurred_at);
END;
$$;
REVOKE ALL ON FUNCTION ops.record_security_event(
    text, text, text, integer, text, text, text, uuid, text, uuid, text, jsonb, timestamptz) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.record_security_event(
    text, text, text, integer, text, text, text, uuid, text, uuid, text, jsonb, timestamptz)
    TO notrelix_app, notrelix_worker;

-- -----------------------------------------------------------------------------
-- Other people's email and phone
-- -----------------------------------------------------------------------------
-- Both mirror the Application rule exactly and add no new one; the Application keeps evaluating its own
-- rule first, these are the bound underneath it.
--
-- Email (accepted 2026-10-23, IMemberEmailDisclosure): only an ACTIVE OWNER or ADMIN of the Workspace reads
-- email, and only of ACTIVE NON-GUEST members of that same Workspace.
CREATE OR REPLACE FUNCTION ops.disclosable_member_emails(p_workspace uuid, p_user_ids uuid[])
RETURNS TABLE (user_id uuid, email text)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT u.id, u.normalized_email::text
      FROM identity.users u
      JOIN workspace.workspace_members subject
        ON subject.user_id = u.id AND subject.workspace_id = p_workspace
     WHERE p_workspace IS NOT NULL
       AND u.id = ANY (p_user_ids)
       AND subject.status = 'Active'
       AND subject.role <> 'Guest'
       AND ops.current_request_scope() IN ('app', 'user')
       AND EXISTS (
            SELECT 1
              FROM workspace.workspace_members reader
             WHERE reader.workspace_id = p_workspace
               AND reader.user_id = ops.current_user_id()
               AND reader.status = 'Active'
               AND reader.role IN ('Owner', 'Admin'));
$$;

-- Phone (D15, IMemberPhoneLookup and its callers): a phone is visible only to an ACTIVE NON-GUEST member of
-- the Workspace, only of ACTIVE NON-GUEST members of it, and only for people who opted in
-- (phone_visible_to_workspace_members). Anyone else, a guest reader or a guest subject included, gets nothing.
CREATE OR REPLACE FUNCTION ops.shared_member_phones(p_workspace uuid, p_user_ids uuid[])
RETURNS TABLE (user_id uuid, phone text)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = pg_catalog, ops, pg_temp
AS $$
    SELECT p.user_id, p.phone::text
      FROM identity.user_profiles p
      JOIN workspace.workspace_members subject
        ON subject.user_id = p.user_id AND subject.workspace_id = p_workspace
     WHERE p_workspace IS NOT NULL
       AND p.user_id = ANY (p_user_ids)
       AND p.phone IS NOT NULL
       AND p.phone_visible_to_workspace_members
       AND subject.status = 'Active'
       AND subject.role <> 'Guest'
       AND ops.current_request_scope() IN ('app', 'user')
       AND EXISTS (
            SELECT 1
              FROM workspace.workspace_members reader
             WHERE reader.workspace_id = p_workspace
               AND reader.user_id = ops.current_user_id()
               AND reader.status = 'Active'
               AND reader.role <> 'Guest');
$$;

REVOKE ALL ON FUNCTION ops.disclosable_member_emails(uuid, uuid[]) FROM PUBLIC;
REVOKE ALL ON FUNCTION ops.shared_member_phones(uuid, uuid[]) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION ops.disclosable_member_emails(uuid, uuid[]) TO notrelix_app;
GRANT EXECUTE ON FUNCTION ops.shared_member_phones(uuid, uuid[]) TO notrelix_app;
